using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using NuGet.Services.Operations.Model;

namespace NuCmd.Commands.Credentials
{
    [Description("Generates a report of the credentials in use across the entire environment")]
    public class ReportCommand : AzureCommandBase
    {
        private static readonly HashSet<string> CredentialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Storage.Primary",
            "Storage.Legacy",
            "Storage.Backup",
            "Sql.Primary",
            "Sql.Legacy",
            "Sql.Warehouse",
            "Gallery.SqlServer",
            "Gallery.AzureStorageConnectionString"
        };

        protected override async Task OnExecute()
        {
            // Fetch config for all services
            var env = GetEnvironment();
            await Console.WriteInfoLine(Strings.Credentials_ReportCommand_CollectingConfiguration);

            // Get the config for each service
            var serviceConfigs = await Task.WhenAll(env.Datacenters.SelectMany(d => d.Services).Select(async s =>
            {
                var config = await LoadServiceConfig(s.Datacenter, s);
                await Console.WriteInfoLine(Strings.Credentials_ReportCommand_RetrievedConfigurationFor, s.Value);
                return new {
                    ServiceName = s.FullName, 
                    Config = config
                };
            }));

            // No John, these AREN'T RDF Triples, these are (service, key, value) triples :)
            var triples = serviceConfigs.SelectMany(serviceConfig => serviceConfig.Config.Where(p => !String.IsNullOrEmpty(p.Value)).Select(pair => new
            {
                ServiceName = serviceConfig.ServiceName,
                Key = pair.Key,
                Type = ExtractType(pair),
                Resource = ExtractResource(serviceConfig.ServiceName, pair),
                Credential = ExtractCredential(pair)
            }))
            .Where(triple => CredentialKeys.Contains(triple.Key));

            // Group by resource and find duplicates
            var keyGroups = triples.GroupBy(t => new { t.Type, t.Resource }).Select(g => new
            {
                Resource = g.Key.Resource,
                Type = g.Key.Type,
                Credentials = g.GroupBy(t => t.Credential)
            });
            var dupes = keyGroups.Where(g => g.Credentials.Count() > 1);
            if (dupes.Any())
            {
                foreach (var dupe in dupes)
                {
                    await Console.WriteErrorLine(Strings.Credentials_ReportCommand_DifferentCredentialsUsedFor, dupe.Resource);
                    foreach (var dupeValue in dupe.Credentials)
                    {
                        await Console.WriteErrorLine(
                            Strings.Credentials_ReportCommand_CredentialValue,
                            dupeValue.Key,
                            String.Join(",", dupeValue.Select(v => v.ServiceName)));
                    }                    
                }
                return;
            }
            await Console.WriteInfoLine(Strings.Credentials_ReportCommand_NoDiffersFound);

            await Console.WriteTable(keyGroups, g => new
            {
                Resource = g.Resource,
                Type = g.Type,
                Services = String.Join(",", g.Credentials.Single().Select(t => t.ServiceName + "(" + t.Key + ")"))
            });
        }

        private string ExtractType(KeyValuePair<string, string> pair)
        {
            if (pair.Key.StartsWith("Storage", StringComparison.OrdinalIgnoreCase) || String.Equals(pair.Key, "Gallery.AzureStorageConnectionString", StringComparison.OrdinalIgnoreCase))
            {
                return "Storage";
            }
            else if (pair.Key.StartsWith("Sql", StringComparison.OrdinalIgnoreCase) || String.Equals(pair.Key, "Gallery.SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                return "Sql";
            }
            return "Unknown";
        }

        private string ExtractResource(string serviceName, KeyValuePair<string, string> pair)
        {
            if (pair.Key.StartsWith("Storage", StringComparison.OrdinalIgnoreCase) || String.Equals(pair.Key, "Gallery.AzureStorageConnectionString", StringComparison.OrdinalIgnoreCase))
            {
                var acct = CloudStorageAccount.Parse(pair.Value);
                return acct.Credentials.AccountName;
            }
            else if (pair.Key.StartsWith("Sql", StringComparison.OrdinalIgnoreCase) || String.Equals(pair.Key, "Gallery.SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var cstr = new SqlConnectionStringBuilder(pair.Value);
                return cstr.DataSource + "/" + cstr.InitialCatalog + "/" + cstr.UserID;
            }
            return String.Empty;
        }

        private string ExtractCredential(KeyValuePair<string, string> pair)
        {
            if (pair.Key.StartsWith("Storage", StringComparison.OrdinalIgnoreCase) || String.Equals(pair.Key, "Gallery.AzureStorageConnectionString", StringComparison.OrdinalIgnoreCase))
            {
                var acct = CloudStorageAccount.Parse(pair.Value);
                return acct.Credentials.ExportBase64EncodedKey();
            }
            else if (pair.Key.StartsWith("Sql", StringComparison.OrdinalIgnoreCase) || String.Equals(pair.Key, "Gallery.SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var cstr = new SqlConnectionStringBuilder(pair.Value);
                return cstr.Password;
            }
            return String.Empty;
        }
    }
}
