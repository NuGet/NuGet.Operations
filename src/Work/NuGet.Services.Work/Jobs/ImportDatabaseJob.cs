using NuGet.Services.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Work.Jobs
{
    [Description("Imports a bacpac file into database")]
    public class ImportDatabaseJob : DatabaseJobHandlerBase<SyncDatacenterEventSource>
    {
        //private static readonly string NorthCentralUSUri = @"https://ch1prod-dacsvc.azure.com/DACWebService.svc";
        private static readonly string EastUSUri = @"https://bl2prod-dacsvc.azure.com/DACWebService.svc";

        public string SourceStorageAccountName { get; set; }

        public string SourceStorageAccountKey { get; set; }

        public string BacpacFile { get; set; }

        public string RequestGUID { get; set; }

        public string EndPointUri { get; set; }

        public ImportDatabaseJob(ConfigurationHub configHub) : base(configHub) { }

        protected internal override Task<JobContinuation> Execute()
        {
            // Load Defaults
            var endPointUri = EndPointUri ?? EastUSUri;
            var cstr = TargetDatabaseConnection;

            if (cstr == null || cstr.InitialCatalog == null || cstr.Password == null || cstr.DataSource == null || cstr.UserID == null)
            {
                throw new ArgumentNullException("One of the connection string parameters or the string itself is null");
            }

            if (SourceStorageAccountName == null)
            {
                throw new ArgumentNullException("Source Storage Account Name is null");
            }

            if (SourceStorageAccountKey == null)
            {
                throw new ArgumentNullException("Source Storage Account Key is null");
            }

            WASDImportExport.ImportExportHelper helper = new WASDImportExport.ImportExportHelper(SyncDatacenterEventSource.Log)
            {
                EndPointUri = endPointUri,
                DatabaseName = TargetDatabaseName ?? cstr.InitialCatalog,
                ServerName = cstr.DataSource,
                UserName = cstr.UserID,
                Password = cstr.Password,
                StorageKey = SourceStorageAccountKey,
            };

            var dotIndex = BacpacFile.IndexOf('.');
            BacpacFile = dotIndex > -1 ? BacpacFile.Substring(0, dotIndex) : BacpacFile;
            var blobAbsoluteUri = String.Format(@"https://{0}.blob.core.windows.net/bacpac-files/{1}.bacpac", SourceStorageAccountName, BacpacFile);

            var requestGUID = helper.DoImport(blobAbsoluteUri, whatIf: WhatIf, async: true);

            if (requestGUID != null)
            {

                Log.Information(String.Format("\n\n Successful Request and Response!! Request GUID is : {0}", requestGUID));

                var parameters = new Dictionary<string, string>();
                parameters["RequestGUID"] = requestGUID;
                parameters["TargetDatabaseConnection"] = cstr.ConnectionString;
                parameters["EndPointUri"] = endPointUri;

                return Task.FromResult(Suspend(TimeSpan.FromMinutes(5), parameters));
            }
            else
            {
                return Task.FromResult(Complete());
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

            var importComplete = false;

            if (statusInfo.Status == "Failed")
            {
                Log.Information(String.Format("After Resuming, Database import failed: {0}", statusInfo.ErrorMessage));
                importComplete = true;
            }

            if (statusInfo.Status == "Completed")
            {
                Log.Information(String.Format("After Resuming, Import Completed - Database has been imported to: {0}", statusInfo.DatabaseName));
                importComplete = true;
            }

            if (importComplete)
            {
                return Task.FromResult(Complete());
            }

            Log.Information(String.Format("Still importing the database. Status : {0}", statusInfo.Status));

            var parameters = new Dictionary<string, string>();
            parameters["RequestGUID"] = RequestGUID;
            parameters["TargetDatabaseConnection"] = TargetDatabaseConnection.ConnectionString;
            parameters["EndPointUri"] = endPointUri;
            return Task.FromResult(Suspend(TimeSpan.FromMinutes(5), parameters));
        }
    }
}
