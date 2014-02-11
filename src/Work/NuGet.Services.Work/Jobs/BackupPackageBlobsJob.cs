using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Dapper;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NuGet.Services.Configuration;
using NuGet.Services.Storage;
using NuGet.Services.Work.Jobs.Models;

namespace NuGet.Services.Work.Jobs
{
    [Description("Creates copies of Package Blobs based on information in the NuGet API v2 Database.")]
    public class BackupPackageBlobsJob : JobHandler<BackupPackageBlobsEventSource>
    {
        public static readonly string BackupStateBlobName = "__backupstate";


        // AzCopy uses this, so it seems good.
        private const int TaskPerCoreFactor = 8;

        /// <summary>
        /// Gets or sets an Azure Storage Uri referring to a container to use as the source for package blobs
        /// </summary>
        public CloudStorageAccount Source { get; set; }
        public string SourceContainerName { get; set; }

        /// <summary>
        /// Gets or sets an Azure Storage Uri referring to a container to use as the destination
        /// </summary>
        public CloudStorageAccount Destination { get; set; }
        public string DestinationContainerName { get; set; }

        /// <summary>
        /// Gets or sets a connection string to the database containing package data.
        /// </summary>
        public SqlConnectionStringBuilder PackageDatabase { get; set; }

        /// <summary>
        /// Run the job in parallel (note: the job is not able to update it's lease on the invocation request in this mode)
        /// </summary>
        public bool RunInParallel { get; set; }

        protected ConfigurationHub Config { get; private set; }
        protected StorageHub Storage { get; private set; }

        protected CloudBlobContainer SourceContainer { get; private set; }
        protected CloudBlobContainer DestinationContainer { get; private set; }

        public BackupPackageBlobsJob(ConfigurationHub config, StorageHub storage)
        {
            Config = config;
            Storage = storage;
        }

        protected internal override async Task Execute()
        {
            var now = DateTimeOffset.UtcNow;

            // Load default data if not provided
            PackageDatabase = PackageDatabase ?? Config.Sql.GetConnectionString(KnownSqlConnection.Legacy);
            Source = Source ?? Config.Storage.Legacy;
            Destination = Destination ?? Config.Storage.Backup;
            SourceContainer = Source.CreateCloudBlobClient().GetContainerReference(
                String.IsNullOrEmpty(SourceContainerName) ? BlobContainerNames.LegacyPackages : SourceContainerName);
            DestinationContainer = Destination.CreateCloudBlobClient().GetContainerReference(
                String.IsNullOrEmpty(DestinationContainerName) ? BlobContainerNames.Backups : DestinationContainerName);
            Log.PreparingToBackup(Source.Credentials.AccountName, SourceContainer.Name, Destination.Credentials.AccountName, DestinationContainer.Name, PackageDatabase.DataSource, PackageDatabase.InitialCatalog);

            // Gather packages
            Log.GatheringListOfPackages(PackageDatabase.DataSource, PackageDatabase.InitialCatalog);
            IList<PackageRef> packages;
            using(var connection = await PackageDatabase.ConnectTo()) {
                packages = (await connection.QueryAsync<PackageRef>(@"
                    SELECT pr.Id, p.NormalizedVersion AS Version, p.Hash
                    FROM Packages p
                    INNER JOIN PackageRegistrations pr ON p.PackageRegistrationKey = pr.[Key]"))
                    .ToList();
            }
            Log.GatheredListOfPackages(packages.Count, PackageDatabase.DataSource, PackageDatabase.InitialCatalog);

            // Collect a list of backups
            Log.GatheringBackupList(Destination.Credentials.AccountName, DestinationContainer.Name);
            var backups = await LoadBackupsList();
            Log.GatheredBackupList(Destination.Credentials.AccountName, DestinationContainer.Name, backups.Count);
            
            // Calculate needed backups
            Log.CalculatingBackupSet(packages.Count, backups.Count);
            var backupSet = CalculateBackupSet(packages, backups);
            Log.CalculatedBackupSet(packages.Count, backupSet.Count);

            if (!WhatIf)
            {
                await DestinationContainer.CreateIfNotExistsAsync();
            }

            Log.StartingBackup(packages.Count);
            if (RunInParallel)
            {
                Parallel.ForEach(
                    backupSet,
                    new ParallelOptions() { MaxDegreeOfParallelism = TaskPerCoreFactor * Environment.ProcessorCount },
                    t => BackupPackage(t.Item1, t.Item2).Wait());
            }
            else
            {
                foreach (var backupRecord in backupSet)
                {
                    await BackupPackage(backupRecord.Item1, backupRecord.Item2);

                    if ((Invocation.NextVisibleAt - DateTimeOffset.UtcNow) < TimeSpan.FromMinutes(1))
                    {
                        // Running out of time! Extend the job
                        Log.ExtendingJobLeaseWhileBackupProgresses();
                        await Extend(TimeSpan.FromMinutes(5));
                        Log.ExtendedJobLease();
                    }
                    else
                    {
                        Log.JobLeaseOk();
                    }
                }
            }
            Log.StartedBackup();
        }

        private IList<Tuple<string, string>> CalculateBackupSet(IList<PackageRef> packages, ISet<string> backups)
        {
            return packages
                .AsParallel()
                .Select(r => Tuple.Create(StorageHelpers.GetPackageBlobName(r), StorageHelpers.GetPackageBackupBlobName(r)))
                .Where(t => !backups.Contains(t.Item2))
                .ToList();
        }

        private async Task BackupPackage(string sourceBlobName, string destinationBlobName)
        {
            // Identify the source and destination blobs
            var sourceBlob = SourceContainer.GetBlockBlobReference(sourceBlobName);
            var destBlob = DestinationContainer.GetBlockBlobReference(destinationBlobName);

            if (await destBlob.ExistsAsync())
            {
                Log.BackupExists(destBlob.Name);
            }
            else if (!await sourceBlob.ExistsAsync())
            {
                Log.SourceBlobMissing(sourceBlob.Name);
            }
            else
            {
                // Start the copy
                Log.StartingCopy(sourceBlob.Name, destBlob.Name);
                if (!WhatIf)
                {
                    await destBlob.StartCopyFromBlobAsync(sourceBlob);
                }
                Log.StartedCopy(sourceBlob.Name, destBlob.Name);
            }
        }

        private async Task<HashSet<string>> LoadBackupsList()
        {
            if (!(await DestinationContainer.ExistsAsync()))
            {
                // No backups!
                return new HashSet<string>();
            }

            var results = new HashSet<string>();
            BlobContinuationToken token = new BlobContinuationToken();
            BlobResultSegment segment;
            var options = new BlobRequestOptions();
            var context = new OperationContext();
            do
            {
                segment = await DestinationContainer.ListBlobsSegmentedAsync(
                    prefix: StorageHelpers.PackageBackupsDirectory + "/",
                    useFlatBlobListing: true,
                    blobListingDetails: BlobListingDetails.None,
                    maxResults: null,
                    currentToken: token,
                    options: options,
                    operationContext: context);
                results.AddRange(
                    segment
                        .Results
                        .OfType<CloudBlockBlob>()
                        .Select(b => b.Name));
                Log.GatheredBackupListSegment(Destination.Credentials.AccountName, DestinationContainer.Name, results.Count);
                token = segment.ContinuationToken;
            } while (token != null);
            return results;
        }
    }

    [EventSource(Name="Outercurve-NuGet-Jobs-BackupPackageBlobs")]
    public class BackupPackageBlobsEventSource : EventSource
    {
        public static readonly BackupPackageBlobsEventSource Log = new BackupPackageBlobsEventSource();
        private BackupPackageBlobsEventSource() { }

        [Event(
            eventId: 1,
            Level = EventLevel.Informational,
            Message = "Preparing to backup package blobs from {0}/{1} to {2}/{3} using package data from {4}/{5}")]
        public void PreparingToBackup(string sourceAccount, string sourceContainer, string destAccount, string destContainer, string dbServer, string dbName) { WriteEvent(1, sourceAccount, sourceContainer, destAccount, destContainer, dbServer, dbName); }

        // [anurse] EventIDs 2 and 3 were removed. There's no need to reuse them and I'd rather keep the event IDs sequential

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
            Message = "Extending job lease while backup progresses")]
        public void ExtendingJobLeaseWhileBackupProgresses() { WriteEvent(6); }

        [Event(
            eventId: 7,
            Level = EventLevel.Informational,
            Task = Tasks.ExtendingJobLease,
            Opcode = EventOpcode.Stop,
            Message = "Extended job lease while backup progresses")]
        public void ExtendedJobLease() { WriteEvent(7); }

        [Event(
            eventId: 8,
            Level = EventLevel.Informational,
            Message = "Backup already exists: {0}")]
        public void BackupExists(string blobName) { WriteEvent(8, blobName); }

        [Event(
            eventId: 9,
            Level = EventLevel.Warning,
            Message = "Source Blob does not exist: {0}")]
        public void SourceBlobMissing(string blobName) { WriteEvent(9, blobName); }

        [Event(
            eventId: 10,
            Level = EventLevel.Informational,
            Task = Tasks.BackingUpPackages,
            Opcode = EventOpcode.Start,
            Message = "Starting backup of {0} packages.")]
        public void StartingBackup(int count) { WriteEvent(10, count); }

        [Event(
            eventId: 11,
            Level = EventLevel.Informational,
            Task = Tasks.BackingUpPackages,
            Opcode = EventOpcode.Stop,
            Message = "Started backups.")]
        public void StartedBackup() { WriteEvent(11); }

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
            Task = Tasks.GatheringBackupList,
            Opcode = EventOpcode.Start,
            Message = "Loading list of available backups from {0}/{1}")]
        public void GatheringBackupList(string destAccount, string destContainer) { WriteEvent(14, destAccount, destContainer); }

        [Event(
            eventId: 15,
            Level = EventLevel.Informational,
            Task = Tasks.GatheringBackupList,
            Opcode = EventOpcode.Stop,
            Message = "Loaded {2} backups from {0}/{1}")]
        public void GatheredBackupList(string destAccount, string destContainer, int count) { WriteEvent(15, destAccount, destContainer, count); }

        [Event(
            eventId: 16,
            Level = EventLevel.Informational,
            Message = "Job lease OK")]
        public void JobLeaseOk() { WriteEvent(16); }

        [Event(
            eventId: 17,
            Level = EventLevel.Informational,
            Task = Tasks.GatheringBackupList,
            Opcode = EventOpcode.Receive,
            Message = "Retrieved {2} backups from {0}/{1}")]
        public void GatheredBackupListSegment(string destAccount, string destContainer, int totalSoFar) { WriteEvent(17, destAccount, destContainer, totalSoFar); }

        [Event(
            eventId: 18,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingBackupSet,
            Opcode = EventOpcode.Start,
            Message = "Calculating Backup Set. Intersecting {0} packages and {1} backups")]
        public void CalculatingBackupSet(int packages, int backups) { WriteEvent(18, packages, backups); }

        [Event(
            eventId: 19,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingBackupSet,
            Opcode = EventOpcode.Stop,
            Message = "Calculated {1} packages out of {0} must be backed up.")]
        public void CalculatedBackupSet(int packages, int toBackup) { WriteEvent(19, packages, toBackup); }

        public static class Tasks
        {
            public const EventTask LoadingBackupState = (EventTask)0x1;
            public const EventTask GatheringPackages = (EventTask)0x2;
            public const EventTask BackingUpPackages = (EventTask)0x3;
            public const EventTask StartingPackageCopy = (EventTask)0x4;
            public const EventTask ExtendingJobLease = (EventTask)0x5;
            public const EventTask GatheringBackupList = (EventTask)0x6;
            public const EventTask CalculatingBackupSet = (EventTask)0x7;
        }
    }
}
