using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Storage;

namespace NuCmd.Commands.Config
{
    public class SummarizeCommand : AzureConnectionCommandBase
    {
        private static readonly Regex NuGetServiceNameMatcher = new Regex(
            @"^(?<product>[A-Za-z]+)-(?<env>[A-Za-z]+)-(?<dc>\d+)-(?<service>.+)$");

        protected override async Task OnExecute(SubscriptionCloudCredentials credentials)
        {
            // List hosted services to collect config
            IList<CloudService> serviceConfigs;
            using (var client = CloudContext.Clients.CreateComputeManagementClient(credentials))
            {
                await Console.WriteInfoLine("Listing cloud services in subscription...");
                var services = (await client.HostedServices.ListAsync()).ToList();
                await Console.WriteInfoLine("Found {0} cloud services. Collecting configuration data...", services.Count);

                var tasks = (
                    from s in services
                    let match = NuGetServiceNameMatcher.Match(s.ServiceName)
                    where match.Success
                    select LoadService(client, s, match)).ToList();

                await Task.WhenAll(tasks);
                serviceConfigs = tasks.Select(t => t.Result).ToList();
            }

            // Time to analyze the data!
            await AnalyzeConfigs(serviceConfigs);
        }

        private async Task<CloudService> LoadService(ComputeManagementClient client, HostedServiceListResponse.HostedService service, Match match)
        {
            await Console.WriteInfoLine("Fetching config for {0}", service.ServiceName);
            var prodTask = GetDeployment(client, service.ServiceName, DeploymentSlot.Production);
            var stageTask = GetDeployment(client, service.ServiceName, DeploymentSlot.Staging);
            await Task.WhenAll(prodTask, stageTask);
            await Console.WriteInfoLine("Loaded config for {0}", service.ServiceName);

            return new CloudService
            {
                Name = service.ServiceName,
                ConfigSettings = Enumerable.Concat(
                    (prodTask.Result == null ?
                        Enumerable.Empty<KeyValuePair<string, string>>() :
                        ParseConfig(prodTask.Result.Configuration)).Select(pair =>
                        new ConfigSetting(service.ServiceName, DeploymentSlot.Production, pair.Key, pair.Value)),
                    (stageTask.Result == null ?
                        Enumerable.Empty<KeyValuePair<string, string>>() :
                        ParseConfig(stageTask.Result.Configuration)).Select(pair =>
                        new ConfigSetting(service.ServiceName, DeploymentSlot.Staging, pair.Key, pair.Value))).ToList()

            };
        }

        private async Task AnalyzeConfigs(IList<CloudService> serviceConfigs)
        {
            var allConfig = serviceConfigs.SelectMany(s => s.ConfigSettings);

            // Pull out storage accounts and sql connections
            IList<ParsedConfigSetting<CloudStorageAccount>> storageConnections = new List<ParsedConfigSetting<CloudStorageAccount>>();
            IList<ParsedConfigSetting<SqlConnectionStringBuilder>> sqlConnections = new List<ParsedConfigSetting<SqlConnectionStringBuilder>>();
            foreach (var setting in allConfig)
            {
                CloudStorageAccount storage;
                if (CloudStorageAccount.TryParse(setting.Value, out storage))
                {
                    storageConnections.Add(new ParsedConfigSetting<CloudStorageAccount>(setting, storage));
                }
                else
                {
                    try
                    {
                        var sql = new SqlConnectionStringBuilder(setting.Value);

                        // Verify it. A lot of strings are valid Connection Strings but don't have the fields we expect :)
                        if (!String.IsNullOrEmpty(sql.InitialCatalog) && !String.IsNullOrEmpty(sql.DataSource))
                        {
                            sqlConnections.Add(new ParsedConfigSetting<SqlConnectionStringBuilder>(setting, sql));
                        }
                    }
                    catch
                    {
                        // Ignore it.
                    }
                }
            }

            // Identify accounts and databases
            var storageAccounts = storageConnections.GroupBy(s => s.Parsed.Credentials.AccountName);
            var sqlDatabases = sqlConnections.GroupBy(s => Tuple.Create(s.Parsed.DataSource, s.Parsed.InitialCatalog, s.Parsed.UserID));

            // Fetch keys for storage accounts in use
            IDictionary<string, string> keys;
            using (var client = CloudContext.Clients.CreateStorageManagementClient(await GetAzureCredentials()))
            {
                var keyTasks = storageAccounts.Select(name => client.StorageAccounts.GetKeysAsync(name)).ToList();
                await Task.WhenAll(keyTasks);

            }


            await Console.WriteInfoLine("Storage Accounts in use:");
            await Console.WriteTable(storageAccounts, a => new
            {
                Account = a.Key,
                UsedBy = String.Join(",", a.Select(c => c.ServiceName).Distinct())
            });

            await Console.WriteInfoLine("SQL Databases in use:");
            await Console.WriteTable(sqlDatabases, a => new
            {
                Server = a.Key.Item1,
                Database = a.Key.Item2,
                User = a.Key.Item3,
                UsedBy = String.Join(",", a.Select(c => c.ServiceName).Distinct())
            });
        }

        private async Task<DeploymentGetResponse> GetDeployment(ComputeManagementClient client, string serviceName, DeploymentSlot slot)
        {
            try
            {
                return await client.Deployments.GetBySlotAsync(serviceName, DeploymentSlot.Production);
            }
            catch (CloudException ex)
            {
                if (ex.ErrorCode == "ResourceNotFound")
                {
                    return null;
                }
                throw;
            }
        }

        private class CloudService
        {
            public string Name
            { get; set; }
            public IList<ConfigSetting> ConfigSettings
            { get; set; }
        }

        private class ConfigSetting
        {
            public ConfigSetting(string serviceName, DeploymentSlot slot, string key, string value)
            {
                ServiceName = serviceName;
                Slot = slot;
                Key = key;
                Value = value;
            }

            public string ServiceName
            { get; private set; }
            public DeploymentSlot Slot
            { get; private set; }
            public string Key
            { get; private set; }
            public string Value
            { get; private set; }
        }

        private class ParsedConfigSetting<T> : ConfigSetting
        {
            public ParsedConfigSetting(ConfigSetting setting, T parsed)
                : base(setting.ServiceName, setting.Slot, setting.Key, setting.Value)
            {
                Parsed = parsed;
            }

            public T Parsed
            { get; set; }
        }
    }
}
