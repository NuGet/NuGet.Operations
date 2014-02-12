using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using NuGet.Services.Configuration;
using NuGet.Services.Work.Jobs.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Work.Jobs
{
    [Description("Imports a bacpac file into database")]
    public class ImportDatabaseJob : DatabaseJobHandlerBase<ImportDatabaseEventSource>
    {
        public static readonly string BackupPrefix = "Backup";
        public string SourceStorageAccountName { get; set; }

        public string SourceStorageAccountKey { get; set; }

        public string BacpacFile { get; set; }

        public string RequestGUID { get; set; }

        public string EndPointUri { get; set; }

        public ImportDatabaseJob(ConfigurationHub configHub) : base(configHub) { }

        protected internal override async Task<JobContinuation> Execute()
        {
            // Load Defaults
            var endPointUri = EndPointUri ?? Config.Sql.ImportEndPoint;
            if (String.IsNullOrEmpty(endPointUri))
            {
                endPointUri = NuGet.Services.Constants.EastUSEndpoint;
            }

            Log.ImportEndpoint(endPointUri);

            var cstr = TargetDatabaseConnection ?? Config.Sql.GetConnectionString(KnownSqlConnection.Primary);
            if (cstr == null || cstr.InitialCatalog == null || cstr.Password == null || cstr.DataSource == null || cstr.UserID == null)
            {
                throw new ArgumentNullException("One of the connection string parameters or the string itself is null");
            }

            if (SourceStorageAccountName == null && SourceStorageAccountKey == null)
            {
                var sourceCredentials = Config.Storage.Primary.Credentials;
                SourceStorageAccountName = sourceCredentials.AccountName;
                SourceStorageAccountKey = sourceCredentials.ExportBase64EncodedKey();
            }
            else
            {
                if (SourceStorageAccountName == null)
                {
                    throw new ArgumentNullException("Source Storage Account Name is null");
                }

                if (SourceStorageAccountKey == null)
                {
                    throw new ArgumentNullException("Source Storage Account Key is null");
                }
            }

            if (String.IsNullOrEmpty(BacpacFile))
            {
                var storageCredentials = new StorageCredentials(SourceStorageAccountName, SourceStorageAccountKey);
                var blobEndPoint = String.Format(@"https://{0}.blob.core.windows.net", SourceStorageAccountName);
                var cloudBlobClient = new CloudBlobClient(new Uri(blobEndPoint), storageCredentials);

                BacpacFile = GetLatestBackupBacpacFile(cloudBlobClient);
            }

            var dotIndex = BacpacFile.IndexOf('.');
            BacpacFile = dotIndex > -1 ? BacpacFile.Substring(0, dotIndex) : BacpacFile;

            TargetDatabaseName = BacpacFile;

            if (await DoesDBExist(cstr, TargetDatabaseName))
            {
                Log.DatabaseAlreadyExists("Database {0} already exists.Skipping...", TargetDatabaseName);
                return Complete();
            }

            WASDImportExport.ImportExportHelper helper = new WASDImportExport.ImportExportHelper()
            {
                EndPointUri = endPointUri,
                DatabaseName = TargetDatabaseName,
                ServerName = cstr.DataSource,
                UserName = cstr.UserID,
                Password = cstr.Password,
                StorageKey = SourceStorageAccountKey,
            };

            var blobAbsoluteUri = String.Format(@"https://{0}.blob.core.windows.net/bacpac-files/{1}.bacpac", SourceStorageAccountName, BacpacFile);

            Log.StartingImport(TargetDatabaseName, cstr.DataSource, BacpacFile);

            var requestGUID = helper.DoImport(Log, blobAbsoluteUri, whatIf: WhatIf);

            if (requestGUID != null)
            {
                Log.ImportStarted(requestGUID);

                var parameters = new Dictionary<string, string>();
                parameters["RequestGUID"] = requestGUID;
                parameters["TargetDatabaseConnection"] = cstr.ConnectionString;
                parameters["EndPointUri"] = endPointUri;

                return Suspend(TimeSpan.FromMinutes(5), parameters);
            }
            else
            {
                throw new Exception("Request to import database unsuccessful. No request GUID obtained");
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
                Log.ImportFailed(statusInfo.ErrorMessage);
                throw new Exception(statusInfo.ErrorMessage);
            }

            if (statusInfo.Status == "Completed")
            {
                Log.ImportCompleted(statusInfo.DatabaseName, helper.ServerName);
                return Task.FromResult(Complete());
            }

            Log.Importing(statusInfo.Status);

            var parameters = new Dictionary<string, string>();
            parameters["RequestGUID"] = RequestGUID;
            parameters["TargetDatabaseConnection"] = TargetDatabaseConnection.ConnectionString;
            parameters["EndPointUri"] = endPointUri;
            return Task.FromResult(Suspend(TimeSpan.FromMinutes(5), parameters));
        }

        private string GetLatestBackupBacpacFile(CloudBlobClient cloudBlobClient)
        {
            try
            {
                // Get a reference to bacpac files container
                var bacpacFileContainer = cloudBlobClient.GetContainerReference(BlobContainerNames.BacpacFiles);

                var blobItems = bacpacFileContainer.ListBlobs(BackupPrefix, useFlatBlobListing: true);
                if (blobItems == null)
                {
                    throw new Exception("No blobs found in bacpacfiles container. That is a mystery!");
                }

                var cloudBlobs = from blobItem in blobItems
                                 where (blobItem as ICloudBlob) != null
                                 select (blobItem as ICloudBlob);

                var latestbacpacFile = (from cloudBlob in cloudBlobs
                                       orderby cloudBlob.Properties.LastModified descending select cloudBlob.Name).FirstOrDefault();
                if (String.IsNullOrEmpty(latestbacpacFile))
                {
                    throw new Exception("No bacpac file with a prefix Backup could be found");
                }

                return latestbacpacFile;
            }
            catch (StorageException storageEx)
            {
                Exception ex = new Exception("Could not obtain a backup bacpac file", storageEx);
                throw ex;
            }
        }

        private async Task<bool> DoesDBExist(SqlConnectionStringBuilder cstr, string name)
        {
            // Connect to the master database
            using (var connection = await cstr.ConnectToMaster())
            {
                var db = await GetDatabase(connection, name);
                return db != null;
            }
        }
    }

    [EventSource(Name = "Outercurve-NuGet-Jobs-ImportDatabase")]
    public class ImportDatabaseEventSource : EventSource
    {
        public static readonly ImportDatabaseEventSource Log = new ImportDatabaseEventSource();

        private ImportDatabaseEventSource() { }

        [Event(
            eventId: 1,
            Level = EventLevel.Informational,
            Message = "Import endpoint is {0}")]
        public void ImportEndpoint(string endPointUri) { WriteEvent(1, endPointUri); }

        [Event(
            eventId: 2,
            Level = EventLevel.Informational,
            Message = "Preparing to import bacpac file {0}")]
        public void PreparingToImport(string bacpacFile) { WriteEvent(2, bacpacFile); }

        [Event(
            eventId: 3,
            Level = EventLevel.Warning,
            Message = "Target database {0} already exists on {1}!")]
        public void DatabaseAlreadyExists(string database, string server) { WriteEvent(3, database, server); }

        [Event(
            eventId: 4,
            Level = EventLevel.Informational,
            Message = "Starting Import of database {0} on server {1} from bacpac file {2}")]
        public void StartingImport(string database, string server, string bacpacFile) { WriteEvent(4, database, server, bacpacFile); }

        [Event(
            eventId: 5,
            Level = EventLevel.Informational,
            Message = "Import has started successfully. Request GUID: {0}")]
        public void ImportStarted(string requestGUID) { WriteEvent(5, requestGUID); }

        [Event(
            eventId: 6,
            Level = EventLevel.Error,
            Message = "Import operation failed. Error Message: {0}")]
        public void ImportFailed(string message) { WriteEvent(6, message); }

        [Event(
            eventId: 7,
            Level = EventLevel.Informational,
            Message = "Import has completed successfully. Database has been imported to {0} on server {1}")]
        public void ImportCompleted(string database, string server) { WriteEvent(7, database, server); }

        [Event(
            eventId: 8,
            Level = EventLevel.Informational,
            Message = "Import is still in progress. Status : {0}")]
        public void Importing(string statusMessage) { WriteEvent(8, statusMessage); }

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
