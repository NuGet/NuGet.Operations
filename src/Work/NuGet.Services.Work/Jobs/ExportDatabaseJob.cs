using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Client;
using NuGet.Services.Configuration;
using NuGet.Services.Work.DACWebService;
using NuGet.Services.Work.Jobs.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage;

namespace NuGet.Services.Work.Jobs
{
    [Description("Exports database from primary datacenter to a bacpac file in secondary datacenter")]
    public class ExportDatabaseJob : DatabaseJobHandlerBase<SyncDatacenterEventSource>
    {
        public static readonly string BackupPrefix = "Backup";
        public string DestinationStorageAccountName { get; set; }
        
        public string DestinationStorageAccountKey { get; set; }

        public string RequestGUID { get; set; }

        public string EndPointUri { get; set; }

        public ExportDatabaseJob(ConfigurationHub configHub) : base(configHub) { }

        protected internal override async Task<JobContinuation> Execute()
        {
            // Load Defaults
            var endPointUri = EndPointUri ?? Config.Sql.ExportEndPoint;
            if (String.IsNullOrEmpty(endPointUri))
            {
                endPointUri = NuGet.Services.Constants.NorthCentralUSEndpoint;
            }

            Log.Information(String.Format("ExportEndpoint is {0}", endPointUri));

            var cstr = TargetDatabaseConnection ?? Config.Sql.GetConnectionString(KnownSqlServer.Legacy, admin: true);
            if (cstr == null || cstr.InitialCatalog == null || cstr.Password == null || cstr.DataSource == null || cstr.UserID == null)
            {
                throw new ArgumentNullException("One of the connection string parameters or the string itself is null");
            }

            if (DestinationStorageAccountKey == null && DestinationStorageAccountName == null)
            {
                var destinationCredentials = Config.Storage.Backup.Credentials;
                DestinationStorageAccountName = destinationCredentials.AccountName;
                DestinationStorageAccountKey = destinationCredentials.ExportBase64EncodedKey();
            }
            else
            {
                if (DestinationStorageAccountName == null)
                {
                    throw new ArgumentNullException("Destination Storage Account Name is null");
                }

                if (DestinationStorageAccountKey == null)
                {
                    throw new ArgumentNullException("Destination Storage Account Key is null");
                }
            }

            TargetDatabaseName = TargetDatabaseName ?? await GetLatestOnlineBackupDatabase(cstr);

            if(TargetDatabaseName == null)
            {
                throw new InvalidOperationException(String.Format("No database with prefix '{0}' was found to export", BackupPrefix));
            }

            WASDImportExport.ImportExportHelper helper = new WASDImportExport.ImportExportHelper(SyncDatacenterEventSource.Log)
            {
                EndPointUri = endPointUri,
                DatabaseName = TargetDatabaseName,
                ServerName = cstr.DataSource,
                UserName = cstr.UserID,
                Password = cstr.Password,
                StorageKey = DestinationStorageAccountKey,
            };

            // Check if the bacpac file is already present
            var storageCredentials = new StorageCredentials(DestinationStorageAccountName, DestinationStorageAccountKey);
            var blobEndPoint = String.Format(@"https://{0}.blob.core.windows.net", DestinationStorageAccountName);
            var cloudBlobClient = new CloudBlobClient(new Uri(blobEndPoint), storageCredentials);

            var bacpacFile = String.Format(@"{0}/{1}/{2}.bacpac", blobEndPoint, BlobContainerNames.BacpacFiles, helper.DatabaseName);

            try
            {
                var blob = await cloudBlobClient.GetBlobReferenceFromServerAsync(new Uri(bacpacFile));
                if (blob != null)
                {
                    Log.Information("Bacpac file already exists. Skipping...");
                    return Complete();
                }
            }
            catch (StorageException)
            {
                // Bacpac file does not exist already. Continue
            }

            var requestGUID = helper.DoExport(bacpacFile, whatIf: WhatIf, async: true);


            if (requestGUID != null)
            {
                Log.Information(String.Format("\n\n Successful Request and Response. Request GUID is : {0}", requestGUID));

                var parameters = new Dictionary<string, string>();
                parameters["RequestGUID"] = requestGUID;
                parameters["TargetDatabaseConnection"] = cstr.ConnectionString;
                parameters["EndPointUri"] = endPointUri;

                return Suspend(TimeSpan.FromMinutes(1), parameters);
            }
            else
            {
                throw new Exception("Request to export database is unsuccessful. No request GUID obtained");
            }
        }

        protected internal override Task<JobContinuation> Resume()
        {
            Log.Information("Resuming ExportDatabase Job...");

            if (RequestGUID == null || TargetDatabaseConnection == null || EndPointUri == null)
            {
                throw new ArgumentNullException("Job could not resume properly due to incorrect parameters");
            }

            var endPointUri = EndPointUri;
            WASDImportExport.ImportExportHelper helper = new WASDImportExport.ImportExportHelper(SyncDatacenterEventSource.Log)
            {
                EndPointUri = endPointUri,
                ServerName = TargetDatabaseConnection.DataSource,
                UserName = TargetDatabaseConnection.UserID,
                Password = TargetDatabaseConnection.Password,
            };

            var statusInfoList = helper.CheckRequestStatus(RequestGUID);
            var statusInfo = statusInfoList.FirstOrDefault();

            if (statusInfo.Status == "Failed")
            {
                var errorMessage = String.Format("After Resuming, Database export failed: {0}", statusInfo.ErrorMessage);
                Log.Information(errorMessage);
                throw new Exception(errorMessage);
            }

            if (statusInfo.Status == "Completed")
            {
                var exportedBlobPath = statusInfo.BlobUri;
                Log.Information(String.Format("After Resuming, Export Completed - Database has been exported to: {0}", exportedBlobPath));
                return Task.FromResult(Complete());
            }

            Log.Information(String.Format("Still exporting the database. Status : {0}", statusInfo.Status));

            var parameters = new Dictionary<string, string>();
            parameters["RequestGUID"] = RequestGUID;
            parameters["TargetDatabaseConnection"] = TargetDatabaseConnection.ConnectionString;
            parameters["EndPointUri"] = endPointUri;
            return Task.FromResult(Suspend(TimeSpan.FromMinutes(1), parameters));
        }

        /// <summary>
        /// Tries to get the latest database with a prefix 'backup'
        /// </summary>
        /// <param name="cstr">Connection string to the server from which the latest database is picked</param>
        /// <returns></returns>
        private async Task<string> GetLatestOnlineBackupDatabase(SqlConnectionStringBuilder cstr)
        {
            // Connect to the master database
            using (var connection = await cstr.ConnectToMaster())
            {
                // Get databases
                var databases = await GetDatabases(connection);

                // Gather backups with matching prefix and order descending
                var ordered = from db in databases
                                let backupMeta = db.GetBackupMetadata()
                                where db.state == DatabaseState.ONLINE && backupMeta != null &&
                                    String.Equals(
                                        BackupPrefix,
                                        backupMeta.Prefix,
                                        StringComparison.OrdinalIgnoreCase)
                                orderby backupMeta.Timestamp descending
                                select backupMeta;

                // Take the most recent one and check it's time
                var mostRecent = ordered.FirstOrDefault();
                if (mostRecent != null)
                {
                    return mostRecent.Db.name;
                }
            }

            return null;
        }
    }

    public class SyncDatacenterEventSource : EventSource
    {
        public static readonly SyncDatacenterEventSource Log = new SyncDatacenterEventSource();

        [Event(
            eventId: 1,
            Level = EventLevel.Informational,
            Message = "Preparing to export source database {0} on server {1} from primary datacenter {2}")]
        public void PreparingToExport(string datacenter, string server, string database) { WriteEvent(1, database, server, datacenter); }

        [Event(
            eventId: 2,
            Level = EventLevel.Warning,
            Message = "Source database {0} not found!")]
        public void SourceDatabaseNotFound(string source) { WriteEvent(2, source); }

        [Event(
            eventId: 3,
            Level = EventLevel.Informational,
            Message = "{0}")]
        public void Information(string message) { WriteEvent(3, message); }

        [Event(
            eventId: 4,
            Level = EventLevel.Error,
            Message = "{0}")]
        public void Error(string message) { WriteEvent(4, message); }
    }
}
