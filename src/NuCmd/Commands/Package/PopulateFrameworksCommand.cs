using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NuGet;
using NuGet.Services.Operations.Model;
using PowerArgs;

namespace NuCmd.Commands.Package
{
    [Description("Populate the Package Frameworks index in the database from the data in the package itself")]
    public class PopulateFrameworksCommand : AzureCommandBase
    {
        [ArgPosition(0)]
        [ArgShortcut("i")]
        [ArgDescription("The ID of the package to process when processing only a single package registration")]
        public string Id { get; set; }

        [ArgPosition(1)]
        [ArgShortcut("v")]
        [ArgDescription("The Version of the package to process")]
        public string Version { get; set; }

        [ArgShortcut("a")]
        [ArgDescription("Set this flag to process all versions of the package when the ID is specified, or all packages if ID is not specified.")]
        public bool All { get; set; }

        [ArgShortcut("db")]
        [ArgDescription("SQL Connection string for the package database.")]
        public string DatabaseConnectionString { get; set; }

        [ArgShortcut("st")]
        [ArgDescription("Azure Storage Connection string for the package storage.")]
        public string StorageConnectionString { get; set; }

        [ArgRequired]
        [ArgShortcut("work")]
        [ArgDescription("Directory in which to put resume data and other work")]
        public string WorkDirectory { get; set; }

        private CloudStorageAccount StorageAccount { get; set; }

        private static readonly int _padLength = Enum.GetValues(typeof(PackageReportState)).Cast<PackageReportState>().Select(p => p.ToString().Length).Max();

        private static readonly JsonSerializer _serializer = new JsonSerializer()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };

        protected override async Task OnExecute()
        {
            if (String.IsNullOrWhiteSpace(Version) && !All)
            {
                await Console.WriteErrorLine(Strings.Package_AllVersionsRequiredIfVersionNull);
                return;
            }

            // Get Datacenter 0
            var dc = GetDatacenter(0, required: false);
            if (dc != null)
            {
                await LoadDefaultsFromAzure(dc);
            }

            StorageAccount = CloudStorageAccount.Parse(StorageConnectionString);

            // Parse the version
            if (!String.IsNullOrWhiteSpace(Version))
            {
                Version = SemanticVersionHelper.Normalize(Version);
            }

            if (!String.IsNullOrWhiteSpace(Version) && All)
            {
                await Console.WriteErrorLine(Strings.Package_VersionAndAllVersionsSpecified);
                return;
            }

            IEnumerable<dynamic> packages;

            // Get all of the packages to be processed
            using (var conn = new SqlConnection(DatabaseConnectionString))
            {
                await conn.OpenAsync();
                packages = conn.Query(@"
                    SELECT      p.[Key], pr.Id, p.Version, p.Hash, p.Created
                    FROM        Packages p
                    INNER JOIN  PackageRegistrations pr
                            ON  pr.[Key] = p.PackageRegistrationKey
                    WHERE       (NullIf(@Id, '') IS NULL OR pr.Id = @Id)
                            AND (@All = 1 OR p.NormalizedVersion = @Version)
                    ORDER BY    p.Created DESC", new
                    {
                        Id,
                        All,
                        Version
                    });
            }

            if (!WhatIf)
            {
                bool confirmed = await Console.Confirm(
                    String.Format(
                        Strings.Package_PopulatePackageFrameworksCommand_Confirm,
                        packages.Count(),
                        (dc == null ? "<unknown>" : dc.FullName)),
                    defaultValue: true);

                if (!confirmed)
                {
                    await Console.WriteErrorLine(Strings.SystemConsole_ConfirmDenied);
                    return;
                }
            }

            int totalCount = packages.Count();
            int processedCount = 0;

            packages
                .AsParallel()
                .AsOrdered()
                // Use 2 threads per processor, because we might find ourselves
                // waiting on SQL
                .WithDegreeOfParallelism(1)
                .ForAll(package =>
                {
                    var thisPackageId = Interlocked.Increment(ref processedCount);
                    ProcessPackage(package, thisPackageId, totalCount);
                });

        }

        private void ProcessPackage(dynamic package, int thisPackageId, int totalCount)
        {
            string countPad = new string('0', totalCount.ToString().Length);

            Console.WriteInfoLine("[{2}/{3} ~{4}%] Processing Package: {0}@{1} (created {5})",
                (string)package.Id,
                (string)package.Version,
                thisPackageId.ToString(countPad),
                totalCount.ToString(countPad),
                (((double)thisPackageId / (double)totalCount) * 100).ToString("000.00"),
                (DateTime)package.Created).Wait();

            try
            {
                var reportPath = Path.Combine(WorkDirectory, package.Id + "_" + package.Version + ".json");
                var bustedReportPath = Path.Combine(WorkDirectory, package.Id + "_" + package.Version + "_" + package.Hash + ".json");

                var report = new PackageFrameworkReport()
                {
                    Id = package.Id,
                    Version = package.Version,
                    Key = package.Key,
                    Hash = package.Hash,
                    Created = package.Created,
                    State = PackageReportState.Unresolved
                };

                if (File.Exists(bustedReportPath))
                {
                    File.Move(bustedReportPath, reportPath);
                }

                using (var conn = new SqlConnection(DatabaseConnectionString))
                {
                    bool resolved = false;

                    if (File.Exists(reportPath))
                    {
                        using (var reader = File.OpenText(reportPath))
                        {
                            var savedReport = (PackageFrameworkReport)_serializer.Deserialize(
                                reader, typeof(PackageFrameworkReport));

                            if (savedReport != null)
                            {
                                report = savedReport;
                                resolved = ResolveReport(report, conn) && report.State == PackageReportState.Resolved;
                            }
                        }
                    }

                    if (!resolved)
                    {
                        try
                        {
                            var downloadPath = DownloadPackage(package);
                            var nugetPackage = new ZipPackage(downloadPath);

                            var supportedFrameworks = GetSupportedFrameworks(nugetPackage);
                            report.PackageFrameworks = supportedFrameworks.ToArray();

                            GetExistingFrameworks((int)(package.Key), report, conn);

                            File.Delete(downloadPath);

                            ResolveReport(report, conn);
                        }
                        catch (Exception ex)
                        {
                            report.State = PackageReportState.Error;
                            report.Error = ex.ToString();
                        }
                    }
                }

                using (var writer = File.CreateText(reportPath))
                {
                    _serializer.Serialize(writer, report);
                }

                Console.WriteInfoLine("[{2}/{3} ~{4}%] {6} Package: {0}@{1} (created {5})",
                    (string)package.Id,
                    (string)package.Version,
                    thisPackageId.ToString(countPad),
                    totalCount.ToString(countPad),
                    (((double)thisPackageId / (double)totalCount) * 100).ToString("000.00"),
                    (DateTime)package.Created,
                    report.State.ToString().PadRight(_padLength, ' ')).Wait();

                if (report.State == PackageReportState.Error)
                {
                    Console.WriteErrorLine("Previous error recorded in report: {0}", report.Error).Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteErrorLine("[{2}/{3} ~{4}%] Error for Package: {0}@{1}: {5}",
                    (string)package.Id,
                    (string)package.Version,
                    thisPackageId.ToString(countPad),
                    totalCount.ToString(countPad),
                    (((double)thisPackageId / (double)totalCount) * 100).ToString("000.00"),
                    ex.ToString()).Wait();
            }
        }

        private bool ResolveReport(PackageFrameworkReport report, SqlConnection conn)
        {
            report.State = PackageReportState.Unresolved;

            if (report.Operations == null)
            {
                return false;
            }

            Console.WriteInfoLine(" {0}@{1} Operations to complete: {2}", report.Id, report.Version, report.Operations.Count()).Wait();

            foreach (var operation in report.Operations)
            {
                if (!WhatIf)
                {
                    if (operation.Type == PackageFrameworkOperationType.Add)
                    {
                        try
                        {
                            conn.Execute(@"
                                INSERT  PackageFrameworks(TargetFramework, Package_Key)
                                VALUES  (@targetFramework, @packageKey)", new
                                {
                                    targetFramework = operation.Framework,
                                    packageKey = report.Key
                                });

                            Console.WriteInfoLine(" + Id={0}, Key={1}, Fx={2}", report.Id, report.Key, operation.Framework).Wait();
                            operation.Applied = true;
                        }
                        catch (Exception ex)
                        {
                            report.State = PackageReportState.Error;
                            operation.Applied = false;
                            operation.Error = ex.ToString();

                            Console.WriteErrorLine(" {0}@{1} '{2}' Error: {3}", report.Id, report.Version, operation.Framework, operation.Error).Wait();
                        }
                    }
                    else if (operation.Type == PackageFrameworkOperationType.Remove)
                    {
                        try
                        {
                            conn.Execute(@"
                                DELETE  PackageFrameworks
                                WHERE   TargetFramework = @targetFramework
                                    AND Package_Key = @packageKey", new
                                {
                                    targetFramework = operation.Framework,
                                    packageKey = report.Key
                                });

                            Console.WriteInfoLine(" - Id={0}, Key={1}, Fx={2}", report.Id, report.Key, operation.Framework).Wait();
                            operation.Applied = true;
                        }
                        catch (Exception ex)
                        {
                            report.State = PackageReportState.Error;
                            operation.Applied = false;
                            operation.Error = ex.ToString();

                            Console.WriteErrorLine(" {0}@{1} '{2}' Error: {3}", report.Id, report.Version, operation.Framework, operation.Error).Wait();
                        }
                    }
                }
            }

            if (report.Operations.All(o => o.Applied))
            {
                report.State = PackageReportState.Resolved;
            }

            return report.State != PackageReportState.Error;
        }

        private string DownloadPackage(dynamic package)
        {
            string id = ((string)package.Id).ToLowerInvariant();
            string version = ((string)package.Version).ToLowerInvariant();
            string hash = WebUtility.UrlEncode((string)package.Hash);

            // Get the blob URL
            var client = StorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference("packages");
            var blob = container.GetBlockBlobReference(
                id + "." + version + ".nupkg");

            var localFile = Path.GetTempFileName();
            if (File.Exists(localFile))
            {
                File.Delete(localFile);
            }

            Console.WriteInfoLine(Strings.Package_DownloadingBlob).Wait();
            blob.DownloadToFile(localFile, FileMode.CreateNew);

            return localFile;
        }

        private static IEnumerable<string> GetSupportedFrameworks(IPackage nugetPackage)
        {
            return nugetPackage.GetSupportedFrameworks().Select(fn =>
                fn == null ? null : VersionUtility.GetShortFrameworkName(fn)).ToArray();                
        }

        private void GetExistingFrameworks(int packageKey, PackageFrameworkReport report, SqlConnection conn)
        {
            // Get all target frameworks in the db for this package
            report.DatabaseFrameworks = new HashSet<string>(conn.Query<string>(@"
                    SELECT  TargetFramework
                    FROM    PackageFrameworks
                    WHERE   Package_Key = @packageKey", new
            {
                packageKey = packageKey
            })).ToArray();

            var adds = report.PackageFrameworks.Except(report.DatabaseFrameworks).Select(targetFramework =>
                new PackageFrameworkOperation()
                {
                    Type = PackageFrameworkOperationType.Add,
                    Framework = targetFramework,
                    Applied = false,
                    Error = "Not Started"
                });
            var rems = report.DatabaseFrameworks.Except(report.PackageFrameworks).Select(targetFramework =>
                new PackageFrameworkOperation()
                {
                    Type = PackageFrameworkOperationType.Remove,
                    Framework = targetFramework,
                    Applied = false,
                    Error = "Not Started"
                });

            report.Operations = Enumerable.Concat(adds, rems).ToArray();
        }

        private async Task LoadDefaultsFromAzure(Datacenter dc)
        {
            bool expired = false;
            try
            {
                if (String.IsNullOrWhiteSpace(DatabaseConnectionString) ||
                    String.IsNullOrWhiteSpace(StorageConnectionString))
                {
                    var config = await LoadServiceConfig(dc, dc.GetService("work"));

                    DatabaseConnectionString = DatabaseConnectionString ??
                        GetValueOrDefault(config, "Sql.Legacy");
                    StorageConnectionString = StorageConnectionString ??
                        GetValueOrDefault(config, "Storage.Legacy");
                }

                if (String.IsNullOrWhiteSpace(DatabaseConnectionString) ||
                    String.IsNullOrWhiteSpace(StorageConnectionString))
                {
                    throw new InvalidOperationException(Strings.Command_MissingEnvironmentArguments);
                }

                await Console.WriteInfoLine(
                    Strings.Command_ConnectionInfo,
                    new SqlConnectionStringBuilder(DatabaseConnectionString).DataSource,
                    CloudStorageAccount.Parse(StorageConnectionString).Credentials.AccountName);
            }
            catch (CloudException ex)
            {
                if (ex.ErrorCode == "AuthenticationFailed")
                {
                    expired = true;
                }
                else
                {
                    throw;
                }
            }

            if (expired)
            {
                await Console.WriteErrorLine(Strings.AzureCommandBase_TokenExpired);
                throw new OperationCanceledException();
            }
        }

        private string GetValueOrDefault(IDictionary<string, string> dict, string key)
        {
            string val;
            if (!dict.TryGetValue(key, out val))
            {
                return null;
            }
            return val;
        }

        public class Package
        {
            public string Hash { get; set; }
            public string Id { get; set; }
            public int Key { get; set; }
            public string Version { get; set; }
            public string NormalizedVersion { get; set; }
            public DateTime Created { get; set; }
        }

        public class PackageFrameworkReport
        {
            public string Id { get; set; }
            public string Version { get; set; }
            public int Key { get; set; }
            public string Hash { get; set; }
            public DateTime Created { get; set; }
            public string[] DatabaseFrameworks { get; set; }
            public string[] PackageFrameworks { get; set; }
            public PackageFrameworkOperation[] Operations { get; set; }
            public string Error { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public PackageReportState State { get; set; }
        }

        public class PackageFrameworkOperation
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public PackageFrameworkOperationType Type { get; set; }
            public string Framework { get; set; }
            public bool Applied { get; set; }
            public string Error { get; set; }
        }

        public enum PackageFrameworkOperationType
        {
            Add,
            Remove
        }

        public enum PackageReportState
        {
            Unresolved,
            Resolved,
            Error
        }
    }
}
