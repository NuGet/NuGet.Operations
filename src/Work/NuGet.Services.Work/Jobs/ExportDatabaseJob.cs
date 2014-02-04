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

namespace NuGet.Services.Work.Jobs
{
    [Description("Exports database from primary datacenter to a bacpac file in secondary datacenter")]
    public class ExportDatabaseJob : DatabaseJobHandlerBase<SyncDatacenterEventSource>
    {
        private static readonly string NorthCentralUSUri = @"https://ch1prod-dacsvc.azure.com/DACWebService.svc";
        //private static readonly string EastUSUri = @"https://bl2prod-dacsvc.azure.com/DACWebService.svc";

        public string DestinationStorageAccountName { get; set; }
        
        public string DestinationStorageAccountKey { get; set; }

        public string RequestGUID { get; set; }

        public string EndPointUri { get; set; }

        public ExportDatabaseJob(ConfigurationHub configHub) : base(configHub) { }

        protected internal override Task<JobContinuation> Execute()
        {
            // Load Defaults
            var endPointUri = EndPointUri ?? NorthCentralUSUri;
            var cstr = TargetDatabaseConnection;

            if (cstr == null || cstr.InitialCatalog == null || cstr.Password == null || cstr.DataSource == null || cstr.UserID == null)
            {
                throw new ArgumentNullException("One of the connection string parameters or the string itself is null");
            }

            if (DestinationStorageAccountName == null)
            {
                throw new ArgumentNullException("Destination Storage Account Name is null");
            }

            if (DestinationStorageAccountKey == null)
            {
                throw new ArgumentNullException("Destination Storage Account Key is null");
            }

            WASDImportExport.ImportExportHelper helper = new WASDImportExport.ImportExportHelper(SyncDatacenterEventSource.Log)
            {
                EndPointUri = endPointUri,
                DatabaseName = TargetDatabaseName ?? cstr.InitialCatalog,
                ServerName = cstr.DataSource,
                UserName = cstr.UserID,
                Password = cstr.Password,
                StorageKey = DestinationStorageAccountKey,
            };

            var blobAbsoluteUri = String.Format(@"https://{0}.blob.core.windows.net/bacpac-files/{1}-{2}.bacpac", DestinationStorageAccountName, helper.DatabaseName, DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss"));

            var requestGUID = helper.DoExport(blobAbsoluteUri, whatIf: WhatIf, async: true);


            if (requestGUID != null)
            {
                Log.Information(String.Format("\n\n Successful Request and Response. Request GUID is : {0}", requestGUID));

                var parameters = new Dictionary<string, string>();
                parameters["RequestGUID"] = requestGUID;
                parameters["TargetDatabaseConnection"] = cstr.ConnectionString;
                parameters["EndPointUri"] = endPointUri;

                return Task.FromResult(Suspend(TimeSpan.FromMinutes(1), parameters));
            }
            else
            {
                return Task.FromResult(Complete());
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

            var exportComplete = false;

            if (statusInfo.Status == "Failed")
            {
                Log.Information(String.Format("After Resuming, Database export failed: {0}", statusInfo.ErrorMessage));
                exportComplete = true;
            }

            if (statusInfo.Status == "Completed")
            {
                var exportedBlobPath = statusInfo.BlobUri;
                Log.Information(String.Format("After Resuming, Export Completed - Database has been exported to: {0}", exportedBlobPath));
                exportComplete = true;
            }

            if (exportComplete)
            {
                return Task.FromResult(Complete());
            }

            Log.Information(String.Format("Still exporting the database. Status : {0}", statusInfo.Status));

            var parameters = new Dictionary<string, string>();
            parameters["RequestGUID"] = RequestGUID;
            parameters["TargetDatabaseConnection"] = TargetDatabaseConnection.ConnectionString;
            parameters["EndPointUri"] = endPointUri;
            return Task.FromResult(Suspend(TimeSpan.FromMinutes(1), parameters));
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
