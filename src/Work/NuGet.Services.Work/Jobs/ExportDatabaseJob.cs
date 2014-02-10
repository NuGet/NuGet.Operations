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
    public class ExportDatabaseJob : DatabaseJobHandlerBase<ExportDatabaseEventSource>
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

            Log.ExportEndpoint(endPointUri);

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

            Log.PreparingToExport(TargetDatabaseName, cstr.DataSource);

            WASDImportExport.ImportExportHelper helper = new WASDImportExport.ImportExportHelper()
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
                    Log.BacpacFileAlreadyExists(bacpacFile);
                    return Complete();
                }
            }
            catch (StorageException)
            {
                // Bacpac file does not exist already. Continue
            }

            Log.StartingExport(TargetDatabaseName, cstr.DataSource, bacpacFile);

            var requestGUID = helper.DoExport(Log, bacpacFile, whatIf: WhatIf);

            if (requestGUID != null)
            {
                Log.ExportStarted(requestGUID);

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
            if (RequestGUID == null || TargetDatabaseConnection == null || EndPointUri == null)
            {
                throw new ArgumentNullException("Job could not resume properly due to incorrect parameters");
            }

            var endPointUri = EndPointUri;
            WASDImportExport.ImportExportHelper helper = new WASDImportExport.ImportExportHelper()
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
                Log.ExportFailed(statusInfo.ErrorMessage);
                throw new Exception(statusInfo.ErrorMessage);
            }

            if (statusInfo.Status == "Completed")
            {
                var exportedBlobPath = statusInfo.BlobUri;
                Log.ExportCompleted(exportedBlobPath);
                return Task.FromResult(Complete());
            }

            Log.Exporting(statusInfo.Status);

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
                var databases = await GetDatabases(connection, DatabaseState.ONLINE);

                // Gather backups with matching prefix and order descending
                var ordered = from db in databases
                                let backupMeta = db.GetBackupMetadata()
                                where backupMeta != null &&
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

    [EventSource(Name="Outercurve-NuGet-Jobs-ExportDatabase")]
    public class ExportDatabaseEventSource : EventSource
    {
        public static readonly ExportDatabaseEventSource Log = new ExportDatabaseEventSource();

        private ExportDatabaseEventSource() { }

        [Event(
            eventId: 1,
            Level = EventLevel.Informational,
            Message= "Export endpoint is {0}")]
        public void ExportEndpoint(string endPointUri) { WriteEvent(1, endPointUri); }

        [Event(
            eventId: 2,
            Level = EventLevel.Informational,
            Message = "Preparing to export source database {0} on server {1}")]
        public void PreparingToExport(string database, string server) { WriteEvent(2, database, server); }

        [Event(
            eventId: 3,
            Level = EventLevel.Warning,
            Message = "Bacpac file {0} already exists!")]
        public void BacpacFileAlreadyExists(string bacpacFile) { WriteEvent(3, bacpacFile); }

        [Event(
            eventId: 4,
            Level = EventLevel.Informational,
            Message = "Starting Export of database {0} on server {1} to bacpac file {2}")]
        public void StartingExport(string database, string server, string bacpacFile) { WriteEvent(4, database, server, bacpacFile); }

        [Event(
            eventId: 5,
            Level = EventLevel.Informational,
            Message = "Export has started successfully. Request GUID: {0}")]
        public void ExportStarted(string requestGUID) { WriteEvent(5, requestGUID); }

        [Event(
            eventId: 6,
            Level = EventLevel.Error,
            Message ="Export operation failed. Error Message: {0}")]
        public void ExportFailed(string message) { WriteEvent(6, message); }

        [Event(
            eventId: 7,
            Level = EventLevel.Informational,
            Message = "Export has completed successfully. Database has been exported to {0}")]
        public void ExportCompleted(string exportedBlobPath) { WriteEvent(7, exportedBlobPath); }

        [Event(
            eventId: 8,
            Level = EventLevel.Informational,
            Message = "Export is still in progress. Status : {0}")]
        public void Exporting(string statusMessage) { WriteEvent(8, statusMessage); }

        [Event(
            eventId: 9,
            Level = EventLevel.Informational,
            Message = "HTTP Posting to requestURI : {0}")]
        public void RequestUri(string requestUri) { WriteEvent(9, requestUri); }

        [Event(
            eventId: 10,
            Level = EventLevel.Informational,
            Message = "Would have sent : {0}")]
        public void WouldHaveSent(string request) { WriteEvent(10, request); }

        [Event(
            eventId: 11,
            Level = EventLevel.Informational,
            Message = "Sending request : {0}")]
        public void SendingRequest(string request) { WriteEvent(11, request); }

        [Event(
            eventId: 12,
            Level = EventLevel.Informational,
            Message = "Request failed. Exception message : {0}")]
        public void RequestFailed(string exceptionMessage) { WriteEvent(12, exceptionMessage); }

        [Event(
            eventId: 13,
            Level = EventLevel.Informational,
            Message = "HttpWebResponse error code. Status : {0}")]
        public void ErrorStatusCode(int statusCode) { WriteEvent(13, statusCode); }

        [Event(
            eventId: 14,
            Level = EventLevel.Informational,
            Message = "HttpWebResponse error description. Status : {0}")]
        public void ErrorStatusDescription(string statusDescription) { WriteEvent(14, statusDescription); }
    }
}
