using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NuGet.Services.Configuration;
using NuGet.Services.Work.Jobs.Models;
using System.Diagnostics.Tracing;

namespace NuGet.Services.Work.Jobs
{
    [Description("Syncs the packages in the failover Datacenter")]
    public class SyncPackagesInFailoverDCJob : JobHandler<SyncPackagesInFailoverDCEventSource>
    {
        // Source storage account has the packages in '{id}/{version}/{packageHash}.nupkg' format
        public CloudStorageAccount Source { get; set; }

        // Destination will act as the packages container for the failover read-only site
        public CloudStorageAccount Destination { get; set; }

        public string SourceContainerName { get; set; }

        public string DestinationContainerName { get; set; }

        protected CloudBlobContainer SourceContainer { get; private set; }
        protected CloudBlobContainer DestinationContainer { get; private set; }

        public SqlConnectionStringBuilder PackageDatabase { get; set; }

        protected ConfigurationHub Config { get; set; }

        public SyncPackagesInFailoverDCJob(ConfigurationHub config)
        {
            Config = config;
        }

        protected internal override async Task Execute()
        {
            PackageDatabase = PackageDatabase ?? Config.Sql.GetConnectionString(KnownSqlConnection.Legacy);
            Source = Source ?? Config.Storage.Backup;
            Destination = Destination ?? Config.Storage.Primary;

            SourceContainer = Source.CreateCloudBlobClient().GetContainerReference(
                String.IsNullOrEmpty(SourceContainerName) ? BlobContainerNames.Backups : SourceContainerName);
            DestinationContainer = Destination.CreateCloudBlobClient().GetContainerReference(
                String.IsNullOrEmpty(DestinationContainerName) ? BlobContainerNames.LegacyPackages : DestinationContainerName);
            Log.PreparingToSync(Source.Credentials.AccountName, SourceContainer.Name, Destination.Credentials.AccountName, DestinationContainer.Name, PackageDatabase.DataSource, PackageDatabase.InitialCatalog);

            // Gather packages
            Log.GatheringListOfPackages(PackageDatabase.DataSource, PackageDatabase.InitialCatalog);
            IList<PackageRefWithLastEdited> packagesInDB;
            using (var connection = await PackageDatabase.ConnectTo())
            {
                packagesInDB = (await connection.QueryAsync<PackageRefWithLastEdited>(@"
                    SELECT pr.Id, p.NormalizedVersion AS Version, p.Hash, p.LastEdited
                    FROM Packages p
                    INNER JOIN PackageRegistrations pr ON p.PackageRegistrationKey = pr.[Key]"))
                    .ToList();
            }
            Log.GatheredListOfPackages(packagesInDB.Count, PackageDatabase.DataSource, PackageDatabase.InitialCatalog);

            if (!WhatIf)
            {
                await DestinationContainer.CreateIfNotExistsAsync();
            }

            // Collect a list of packages in destination with metadata
            Log.GatheringDestinationPackages(Destination.BlobEndpoint.ToString(), DestinationContainer.Name);
            var destinationPackages = await LoadPackagesAtDestination();
            Log.GatheredDestinationPackagesList(Destination.BlobEndpoint.ToString(), DestinationContainer.Name, destinationPackages.Count);

            Log.CalculatingCopyOrOverwritePackages(packagesInDB.Count, destinationPackages.Count);
            var packagesToCopyOrOverwrite = PackagesToCopyOrOverwrite(packagesInDB, destinationPackages);
            Log.CalculatedCopyOrOverwritePackages(packagesInDB.Count, packagesToCopyOrOverwrite.Count);

            Log.StartingSync(packagesToCopyOrOverwrite.Count);
            if (packagesToCopyOrOverwrite.Count > 0)
            {
                var policy = new SharedAccessBlobPolicy();
                policy.SharedAccessStartTime = DateTimeOffset.Now;
                policy.SharedAccessExpiryTime = DateTimeOffset.Now + TimeSpan.FromHours(2) + TimeSpan.FromMinutes(2 * packagesToCopyOrOverwrite.Count);
                policy.Permissions = SharedAccessBlobPermissions.Read;
                var sourceContainerSharedAccessUri = SourceContainer.GetSharedAccessSignature(policy);
                Log.SharedAccessSignatureURI(sourceContainerSharedAccessUri);

                foreach (var packageRef in packagesToCopyOrOverwrite)
                {
                    await CopyOrOverwritePackage(sourceContainerSharedAccessUri, StorageHelpers.GetPackageBackupBlobName(packageRef),
                        StorageHelpers.GetPackageBlobName(packageRef), packageRef.Hash);

                    if ((Invocation.NextVisibleAt - DateTimeOffset.UtcNow) < TimeSpan.FromMinutes(1))
                    {
                        // Running out of time! Extend the job
                        Log.ExtendingJobLeaseWhileSyncingProgresses();
                        await Extend(TimeSpan.FromMinutes(5));
                        Log.ExtendedJobLease();
                    }
                    else
                    {
                        Log.JobLeaseOk();
                    }
                }
            }

            Log.CalculatingDeletePackages(destinationPackages.Count, packagesInDB.Count);
            var packagesToDelete = PackagesToDelete(packagesInDB, destinationPackages);
            Log.CalculatedDeletePackages(packagesToDelete.Count, destinationPackages.Count);

            if (packagesToDelete.Count > 0)
            {
                foreach (var packageBlobName in packagesToDelete)
                {
                    await DeletePackage(packageBlobName);

                    if ((Invocation.NextVisibleAt - DateTimeOffset.UtcNow) < TimeSpan.FromMinutes(1))
                    {
                        // Running out of time! Extend the job
                        Log.ExtendingJobLeaseWhileSyncingProgresses();
                        await Extend(TimeSpan.FromMinutes(5));
                        Log.ExtendedJobLease();
                    }
                    else
                    {
                        Log.JobLeaseOk();
                    }
                }
            }
            Log.StartedSync();
        }

        private async Task<Dictionary<string, DateTimeOffset>> LoadPackagesAtDestination()
        {
            var results = new Dictionary<string, DateTimeOffset>();
            BlobContinuationToken token = new BlobContinuationToken();
            BlobResultSegment segment;
            var options = new BlobRequestOptions();
            var context = new OperationContext();
            do
            {
                segment = await DestinationContainer.ListBlobsSegmentedAsync(
                    prefix: null,
                    useFlatBlobListing: true,
                    blobListingDetails: BlobListingDetails.Metadata,
                    maxResults: null,
                    currentToken: token,
                    options: options,
                    operationContext: context);

                results.AddRange(
                    segment
                        .Results
                        .OfType<CloudBlockBlob>()
                        .Select(b => new KeyValuePair<string, DateTimeOffset>(b.Name, b.Properties.LastModified.HasValue ? b.Properties.LastModified.Value : DateTimeOffset.Now)));

                Log.GatheredDestinationPackagesListSegment(Destination.Credentials.AccountName, DestinationContainer.Name, results.Count);
                token = segment.ContinuationToken;
            } while (token != null);
            return results;
        }

        private IList<PackageRefWithLastEdited> PackagesToCopyOrOverwrite(IList<PackageRefWithLastEdited> packagesInDB, Dictionary<string, DateTimeOffset> packages)
        {
            var packagesToCopy = packagesInDB.Where(pkgRef =>
                {
                    var blobName = StorageHelpers.GetPackageBlobName(pkgRef);
                    DateTimeOffset lastModified;
                    if (packages.TryGetValue(blobName, out lastModified))
                    {
                        if (pkgRef.LastEdited == null)
                        {
                            // Package Exists and Don't overwrite
                            // LastEdited is null meaning packages was not edited after getting published
                            return false;
                        }
                        else
                        {
                            // Overwrite if LastEdited of the package is greater than the last modified of the package blob
                            return lastModified < pkgRef.LastEdited.ToUniversalTime();
                        }
                    }
                    else
                    {
                        // Package does not exist at all
                        // Copy the package
                        return true;
                    }
                });

            return packagesToCopy.ToList();
        }

        private IList<string> PackagesToDelete(IList<PackageRefWithLastEdited> packagesInDB, Dictionary<string, DateTimeOffset> packages)
        {
            var packagesInDBAsFilenames = new HashSet<string>(from pkgRef in packagesInDB
                                                              select StorageHelpers.GetPackageBlobName(pkgRef));

            return (from pkg in packages.Where(p => !packagesInDBAsFilenames.Contains(p.Key))
                   select pkg.Key).ToList();
        }

        protected async Task CopyOrOverwritePackage(string sourceContainerSharedAccessUri, string sourceBlobName, string destinationBlobName, string packageHash)
        {
            // Identify the source and destination blobs
            var sourceBlob = SourceContainer.GetBlockBlobReference(sourceBlobName);
            var destBlob = DestinationContainer.GetBlockBlobReference(destinationBlobName);

            // If the destination blob already exists, it will be overwritten
            // To prevent several requests, do not check if it exists

            if (!await sourceBlob.ExistsAsync())
            {
                Log.SourceBlobMissing(sourceBlob.Name);
            }
            else
            {
                var sourceUri = new Uri(sourceBlob.Uri, sourceContainerSharedAccessUri);
                // Start the copy or overwrite
                Log.StartingCopy(sourceUri.AbsoluteUri, destBlob.Uri.AbsoluteUri);
                if (!WhatIf)
                {
                    await destBlob.StartCopyFromBlobAsync(sourceUri);
                }
                Log.StartedCopy(sourceUri.AbsoluteUri, destBlob.Uri.AbsoluteUri);
            }
        }

        protected async Task DeletePackage(string blobName)
        {
            var blob = DestinationContainer.GetBlockBlobReference(blobName);
            Log.StartingDelete(blob.Uri.AbsoluteUri);
            if (!WhatIf)
            {
                await blob.DeleteIfExistsAsync();
            }
            Log.StartedDelete(blob.Uri.AbsoluteUri);
        }
    }

    [EventSource(Name = "Outercurve-NuGet-Jobs-SyncPackagesInFailoverDC")]
    public class SyncPackagesInFailoverDCEventSource : EventSource
    {
        public static readonly SyncPackagesInFailoverDCEventSource Log = new SyncPackagesInFailoverDCEventSource();
        private SyncPackagesInFailoverDCEventSource() { }

        [Event(
            eventId: 1,
            Level = EventLevel.Informational,
            Message = "Preparing to sync package blobs from {0}/{1} to {2}/{3} using package data from {4}/{5}")]
        public void PreparingToSync(string sourceAccount, string sourceContainer, string destAccount, string destContainer, string dbServer, string dbName) { WriteEvent(1, sourceAccount, sourceContainer, destAccount, destContainer, dbServer, dbName); }

        [Event(
            eventId: 4,
            Level = EventLevel.Informational,
            Task = Tasks.GatheringPackages,
            Opcode = EventOpcode.Start,
            Message = "Gathering list of packages from {0}/{1}")]
        public void GatheringListOfPackages(string dbServer, string dbName) { WriteEvent(4, dbServer, dbName); }

        [Event(
            eventId: 5,
            Level = EventLevel.Informational,
            Task = Tasks.GatheringPackages,
            Opcode = EventOpcode.Stop,
            Message = "Gathered {0} packages from {1}/{2}")]
        public void GatheredListOfPackages(int gathered, string dbServer, string dbName) { WriteEvent(5, gathered, dbServer, dbName); }

        [Event(
            eventId: 6,
            Level = EventLevel.Informational,
            Task = Tasks.ExtendingJobLease,
            Opcode = EventOpcode.Start,
            Message = "Extending job lease while syncing progresses")]
        public void ExtendingJobLeaseWhileSyncingProgresses() { WriteEvent(6); }

        [Event(
            eventId: 7,
            Level = EventLevel.Informational,
            Task = Tasks.ExtendingJobLease,
            Opcode = EventOpcode.Stop,
            Message = "Extended job lease while syncing progresses")]
        public void ExtendedJobLease() { WriteEvent(7); }

        [Event(
            eventId: 8,
            Level = EventLevel.Informational,
            Message = "Destination blob {0} already exists. It will be overwritten")]
        public void DestinationBlobExists(string blobName) { WriteEvent(8, blobName); }

        [Event(
            eventId: 9,
            Level = EventLevel.Warning,
            Message = "Source Blob does not exist: {0}")]
        public void SourceBlobMissing(string blobName) { WriteEvent(9, blobName); }

        [Event(
            eventId: 10,
            Level = EventLevel.Informational,
            Task = Tasks.SyncingPackages,
            Opcode = EventOpcode.Start,
            Message = "Starting syncing of {0} packages.")]
        public void StartingSync(int count) { WriteEvent(10, count); }

        [Event(
            eventId: 11,
            Level = EventLevel.Informational,
            Task = Tasks.SyncingPackages,
            Opcode = EventOpcode.Stop,
            Message = "Started syncing.")]
        public void StartedSync() { WriteEvent(11); }

        [Event(
            eventId: 12,
            Level = EventLevel.Informational,
            Task = Tasks.StartingPackageCopy,
            Opcode = EventOpcode.Start,
            Message = "Starting copy of {0} to {1}.")]
        public void StartingCopy(string source, string dest) { WriteEvent(12, source, dest); }

        [Event(
            eventId: 13,
            Level = EventLevel.Informational,
            Task = Tasks.StartingPackageCopy,
            Opcode = EventOpcode.Stop,
            Message = "Started copy of {0} to {1}.")]
        public void StartedCopy(string source, string dest) { WriteEvent(13, source, dest); }

        [Event(
            eventId: 14,
            Level = EventLevel.Informational,
            Task = Tasks.GatheringDestinationPackagesList,
            Opcode = EventOpcode.Start,
            Message = "Loading packages in destination: {0}/{1}")]
        public void GatheringDestinationPackages(string destAccount, string destContainer) { WriteEvent(14, destAccount, destContainer); }

        [Event(
            eventId: 15,
            Level = EventLevel.Informational,
            Task = Tasks.GatheringDestinationPackagesList,
            Opcode = EventOpcode.Stop,
            Message = "Loaded {2} packages from {0}/{1}")]
        public void GatheredDestinationPackagesList(string destAccount, string destContainer, int count) { WriteEvent(15, destAccount, destContainer, count); }

        [Event(
            eventId: 16,
            Level = EventLevel.Informational,
            Message = "Job lease OK")]
        public void JobLeaseOk() { WriteEvent(16); }

        [Event(
            eventId: 17,
            Level = EventLevel.Informational,
            Task = Tasks.GatheringDestinationPackagesList,
            Opcode = EventOpcode.Receive,
            Message = "Retrieved {2} packages from destination {0}/{1}")]
        public void GatheredDestinationPackagesListSegment(string destAccount, string destContainer, int totalSoFar) { WriteEvent(17, destAccount, destContainer, totalSoFar); }

        [Event(
            eventId: 18,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingCopyOrOverwritePackages,
            Opcode = EventOpcode.Start,
            Message = "Calculating packages to CopyOrOverwrite. Intersecting {0} packages in DB and {1} packages in destination")]
        public void CalculatingCopyOrOverwritePackages(int packagesInDB, int destinationPackages) { WriteEvent(18, packagesInDB, destinationPackages); }

        [Event(
            eventId: 19,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingCopyOrOverwritePackages,
            Opcode = EventOpcode.Stop,
            Message = "Calculated that {1} packages out of {0} must be synced")]
        public void CalculatedCopyOrOverwritePackages(int packagesInDB, int toCopyOrOverwrite) { WriteEvent(19, packagesInDB, toCopyOrOverwrite); }

        [Event(
            eventId: 20,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingCopyOrOverwritePackages,
            Opcode = EventOpcode.Info,
            Message = "Container's Shared Access signature is {0}")]
        public void SharedAccessSignatureURI(string sharedAccessSig) { WriteEvent(20, sharedAccessSig); }

        [Event(
            eventId: 21,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingDeletePackages,
            Opcode = EventOpcode.Start,
            Message = "Calculating packages to delete. Intersecting {0} packages in destination and {1} packages in DB")]
        public void CalculatingDeletePackages(int destinationPackages, int packagesInDB) { WriteEvent(21, destinationPackages, packagesInDB); }

        [Event(
            eventId: 22,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingDeletePackages,
            Opcode = EventOpcode.Stop,
            Message = "Calculated that {0} packages out of {1} must be deleted")]
        public void CalculatedDeletePackages(int packagesToDelete, int destinationPackages) { WriteEvent(22, packagesToDelete, destinationPackages); }

        [Event(
            eventId: 23,
            Level = EventLevel.Informational,
            Task = Tasks.StartingPackageDelete,
            Opcode = EventOpcode.Start,
            Message = "Starting deletion of package {0}")]
        public void StartingDelete(string package) { WriteEvent(23, package); }

        [Event(
            eventId: 24,
            Level = EventLevel.Informational,
            Task = Tasks.StartingPackageDelete,
            Opcode = EventOpcode.Stop,
            Message = "Started deletion of package {0}")]
        public void StartedDelete(string package) { WriteEvent(24, package); }

        public static class Tasks
        {
            public const EventTask GatheringPackages = (EventTask)0x2;
            public const EventTask SyncingPackages = (EventTask)0x3;
            public const EventTask StartingPackageCopy = (EventTask)0x4;
            public const EventTask StartingPackageDelete = (EventTask)0x5;
            public const EventTask ExtendingJobLease = (EventTask)0x6;
            public const EventTask GatheringDestinationPackagesList = (EventTask)0x7;
            public const EventTask CalculatingCopyOrOverwritePackages = (EventTask)0x8;
            public const EventTask CalculatingDeletePackages = (EventTask)0x9;
        }
    }
}
