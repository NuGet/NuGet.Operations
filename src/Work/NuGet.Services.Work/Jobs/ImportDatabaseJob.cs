using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using NuGet.Services.Configuration;
using NuGet.Services.Work.Jobs.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Work.Jobs
{
    // NOTE: THIS JOB IS INCOMPLETE. THIS NEEDS TO BE UPDATED TO USE CLOUD CONFIGURATION SETTINGS
    // AND, MOST IMPORTANTLY, THIS JOB NEEDS TO BE RUN ON DATACENTER 1 and NOT ON DATACENTER 0
    [Description("Imports a bacpac file into database")]
    public class ImportDatabaseJob : DatabaseJobHandlerBase<SyncDatacenterEventSource>
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

            Log.Information(String.Format("ImportEndpoint is {0}", endPointUri));

            var cstr = TargetDatabaseConnection ?? Config.Sql.GetConnectionString(KnownSqlServer.Primary, admin: true);
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
                Log.Information(String.Format("Database {0} already exists.Skipping...", TargetDatabaseName));
                return Complete();
            }

            WASDImportExport.ImportExportHelper helper = new WASDImportExport.ImportExportHelper(SyncDatacenterEventSource.Log)
            {
                EndPointUri = endPointUri,
                DatabaseName = TargetDatabaseName,
                ServerName = cstr.DataSource,
                UserName = cstr.UserID,
                Password = cstr.Password,
                StorageKey = SourceStorageAccountKey,
            };

            var blobAbsoluteUri = String.Format(@"https://{0}.blob.core.windows.net/bacpac-files/{1}.bacpac", SourceStorageAccountName, BacpacFile);

            var requestGUID = helper.DoImport(blobAbsoluteUri, whatIf: WhatIf, async: true);

            if (requestGUID != null)
            {
                Log.Information(String.Format("\n\n Successful Request and Response!! Request GUID is : {0}", requestGUID));

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
            Log.Information("Resuming ImportDatabase Job...");

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
                var errorMessage = String.Format("After Resuming, Database import failed: {0}", statusInfo.ErrorMessage);
                Log.Information(errorMessage);
                throw new Exception(errorMessage);
            }

            if (statusInfo.Status == "Completed")
            {
                Log.Information(String.Format("After Resuming, Import Completed - Database has been imported to: {0}", statusInfo.DatabaseName));
                return Task.FromResult(Complete());
            }

            Log.Information(String.Format("Still importing the database. Status : {0}", statusInfo.Status));

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
}
