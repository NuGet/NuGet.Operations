using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NuGet.Services.Configuration;

namespace NuGet.Services.Work.Jobs
{
    [Description("Transfer backup packages in primary DC to failover DC")]
    public class TransferBackupPackagesJob : JobHandler<TransferBackupPackagesEventSource>
    {
        // Source storage account has the packages in '{id}/{version}/{packageHash}.nupkg' format
        public CloudStorageAccount Source { get; set; }

        // Destination storage account has the same format as Source '{id}/{version}/{packageHash}.nupkg'
        public CloudStorageAccount Destination { get; set; }

        public string SourceContainerName { get; set; }

        public string DestinationContainerName { get; set; }

        protected CloudBlobContainer SourceContainer { get; private set; }
        protected CloudBlobContainer DestinationContainer { get; private set; }

        protected ConfigurationHub Config { get; set; }

        public TransferBackupPackagesJob(ConfigurationHub config)
        {
            Config = config;
        }

        protected internal override async Task Execute()
        {
            Source = Source ?? Config.Storage.Primary;
            Destination = Destination ?? Config.Storage.Backup;

            SourceContainer = Source.CreateCloudBlobClient().GetContainerReference(
                String.IsNullOrEmpty(SourceContainerName) ? BlobContainerNames.Backups : SourceContainerName);
            DestinationContainer = Destination.CreateCloudBlobClient().GetContainerReference(
                String.IsNullOrEmpty(DestinationContainerName) ? BlobContainerNames.Backups : DestinationContainerName);
            Log.PreparingToTransfer(Source.Credentials.AccountName, SourceContainer.Name, Destination.Credentials.AccountName, DestinationContainer.Name);

            // Gather packages
            Log.GatheringListOfPackages(Source.BlobEndpoint.ToString(), SourceContainer.Name);
            var sourcePackages = await LoadBlobList(Log, Source, SourceContainer);
            Log.GatheredListOfPackages(sourcePackages.Count, Source.BlobEndpoint.ToString(), SourceContainer.Name);

            if (!WhatIf)
            {
                await DestinationContainer.CreateIfNotExistsAsync();
            }

            // Collect a list of packages in destination with metadata
            Log.GatheringListOfPackages(Destination.BlobEndpoint.ToString(), DestinationContainer.Name);
            var destinationPackages = await LoadBlobList(Log, Destination, DestinationContainer);
            Log.GatheredListOfPackages(destinationPackages.Count, Destination.BlobEndpoint.ToString(), DestinationContainer.Name);

            Log.CalculatingCopyPackages(sourcePackages.Count, destinationPackages.Count);
            var packageBlobsToCopy = sourcePackages.Where(p => !destinationPackages.Contains(p)).ToList();
            Log.CalculatedCopyPackages(sourcePackages.Count, packageBlobsToCopy.Count);

            Log.StartingTransfer(packageBlobsToCopy.Count);

            if (packageBlobsToCopy.Count > 0)
            {
                var policy = new SharedAccessBlobPolicy();
                policy.SharedAccessStartTime = DateTimeOffset.Now;
                policy.SharedAccessExpiryTime = DateTimeOffset.Now + TimeSpan.FromHours(2) + TimeSpan.FromMinutes(2 * packageBlobsToCopy.Count);
                policy.Permissions = SharedAccessBlobPermissions.Read;
                var sourceContainerSharedAccessUri = SourceContainer.GetSharedAccessSignature(policy);
                Log.SharedAccessSignatureURI(sourceContainerSharedAccessUri);

                foreach (var packageBlobName in packageBlobsToCopy)
                {
                    await CopyPackageToDestination(sourceContainerSharedAccessUri, packageBlobName);

                    if ((Invocation.NextVisibleAt - DateTimeOffset.UtcNow) < TimeSpan.FromMinutes(1))
                    {
                        // Running out of time! Extend the job
                        Log.ExtendingJobLeaseWhileTransferProgresses();
                        await Extend(TimeSpan.FromMinutes(5));
                        Log.ExtendedJobLease();
                    }
                    else
                    {
                        Log.JobLeaseOk();
                    }
                }
            }

            Log.StartedTransfer();
        }

        private async Task CopyPackageToDestination(string sourceContainerSharedAccessUri, string packageBlobName)
        {
            // Identify the source and destination blobs
            var sourceBlob = SourceContainer.GetBlockBlobReference(packageBlobName);
            var destBlob = DestinationContainer.GetBlockBlobReference(packageBlobName);

            // If the destination blob already exists, it will be overwritten. NO harm done
            // While it should not happen, to prevent several requests, do not check if it exists

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

        private static async Task<HashSet<string>> LoadBlobList(TransferBackupPackagesEventSource log, CloudStorageAccount account, CloudBlobContainer container)
        {
            if (!(await container.ExistsAsync()))
            {
                // No packages!
                return new HashSet<string>();
            }

            var results = new HashSet<string>();
            BlobContinuationToken token = new BlobContinuationToken();
            BlobResultSegment segment;
            var options = new BlobRequestOptions();
            var context = new OperationContext();
            do
            {
                segment = await container.ListBlobsSegmentedAsync(
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
                log.GatheredPackagesListSegment(account.Credentials.AccountName, container.Name, results.Count);
                token = segment.ContinuationToken;
            } while (token != null);
            return results;
        }
    }

    [EventSource(Name = "Outercurve-NuGet-Jobs-TransferBackupPackages")]
    public class TransferBackupPackagesEventSource : EventSource
    {
        public static readonly TransferBackupPackagesEventSource Log = new TransferBackupPackagesEventSource();

        private TransferBackupPackagesEventSource() { }

        [Event(
            eventId: 1,
            Level = EventLevel.Informational,
            Message = "Preparing to transfer package blobs from {0}/{1} to {2}/{3}")]
        public void PreparingToTransfer(string sourceAccount, string sourceContainer, string destAccount, string destContainer) { WriteEvent(1, sourceAccount, sourceContainer, destAccount, destContainer); }

        [Event(
            eventId: 4,
            Level = EventLevel.Informational,
            Task = Tasks.GatheringPackages,
            Opcode = EventOpcode.Start,
            Message = "Gathering list of packages from account {0}/{1}")]
        public void GatheringListOfPackages(string account, string container) { WriteEvent(4, account, container); }

        [Event(
            eventId: 5,
            Level = EventLevel.Informational,
            Task = Tasks.GatheringPackages,
            Opcode = EventOpcode.Stop,
            Message = "Gathered {0} packages from account {1}/{2}")]
        public void GatheredListOfPackages(int gathered, string account, string container) { WriteEvent(5, gathered, account, container); }

        [Event(
            eventId: 6,
            Level = EventLevel.Informational,
            Task = Tasks.ExtendingJobLease,
            Opcode = EventOpcode.Start,
            Message = "Extending job lease while transfer progresses")]
        public void ExtendingJobLeaseWhileTransferProgresses() { WriteEvent(6); }

        [Event(
            eventId: 7,
            Level = EventLevel.Informational,
            Task = Tasks.ExtendingJobLease,
            Opcode = EventOpcode.Stop,
            Message = "Extended job lease while transfer progresses")]
        public void ExtendedJobLease() { WriteEvent(7); }

        [Event(
            eventId: 9,
            Level = EventLevel.Warning,
            Message = "Source Blob does not exist: {0}")]
        public void SourceBlobMissing(string blobName) { WriteEvent(9, blobName); }

        [Event(
            eventId: 10,
            Level = EventLevel.Informational,
            Task = Tasks.TransferringPackages,
            Opcode = EventOpcode.Start,
            Message = "Starting transfer of {0} packages.")]
        public void StartingTransfer(int count) { WriteEvent(10, count); }

        [Event(
            eventId: 11,
            Level = EventLevel.Informational,
            Task = Tasks.TransferringPackages,
            Opcode = EventOpcode.Stop,
            Message = "Started transfer.")]
        public void StartedTransfer() { WriteEvent(11); }

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
        public void GatheredPackagesListSegment(string account, string container, int totalSoFar) { WriteEvent(17, account, container, totalSoFar); }

        [Event(
            eventId: 18,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingCopyPackages,
            Opcode = EventOpcode.Start,
            Message = "Calculating packages to copy. Intersecting {0} packages in source and {1} packages in destination")]
        public void CalculatingCopyPackages(int sourcePackages, int destinationPackages) { WriteEvent(18, sourcePackages, destinationPackages); }

        [Event(
            eventId: 19,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingCopyPackages,
            Opcode = EventOpcode.Stop,
            Message = "Calculated that {1} packages out of {0} must be copied")]
        public void CalculatedCopyPackages(int sourcePackages, int toCopy) { WriteEvent(19, sourcePackages, toCopy); }

        [Event(
            eventId: 20,
            Level = EventLevel.Informational,
            Task = Tasks.CalculatingCopyPackages,
            Opcode = EventOpcode.Info,
            Message = "Container's Shared Access signature is {0}")]
        public void SharedAccessSignatureURI(string sharedAccessSig) { WriteEvent(20, sharedAccessSig); }

        public static class Tasks
        {
            public const EventTask GatheringPackages = (EventTask)0x1;
            public const EventTask TransferringPackages = (EventTask)0x2;
            public const EventTask StartingPackageCopy = (EventTask)0x3;
            public const EventTask ExtendingJobLease = (EventTask)0x4;
            public const EventTask GatheringDestinationPackagesList = (EventTask)0x5;
            public const EventTask CalculatingCopyPackages = (EventTask)0x6;
        }
    }

}
