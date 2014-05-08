using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute.Models;
using NuGet.Services.Operations.Model;
using PowerArgs;

namespace NuCmd.Commands
{
    public abstract class EnvironmentCommandBase : Command
    {
        [ArgShortcut("e")]
        [ArgDescription("The environment to work in (defaults to the current environment)")]
        public string Environment { get; set; }

        protected virtual DeploymentEnvironment GetEnvironment(bool required)
        {
            return GetEnvironment(Environment, required);
        }

        protected virtual DeploymentEnvironment GetEnvironment()
        {
            return GetEnvironment(Environment);
        }

        protected virtual Datacenter GetDatacenter(int datacenter)
        {
            return GetDatacenter(datacenter, required: true);
        }

        protected virtual Datacenter GetDatacenter(int datacenter, bool required)
        {
            var env = GetEnvironment(required);
            if (env == null)
            {
                return null;
            }
            return GetDatacenter(env, datacenter, required);
        }

        protected async Task<SubscriptionCloudCredentials> GetAzureCredentials()
        {
            if (Session == null ||
                Session.CurrentEnvironment == null ||
                Session.CurrentEnvironment.Subscription == null)
            {
                throw new InvalidOperationException(Strings.AzureCommandBase_RequiresSubscription);
            }

            var token = await Session.AzureTokens.LoadToken(Session.CurrentEnvironment.Subscription.Id);
            if (token == null)
            {
                throw new InvalidOperationException(Strings.AzureCommandBase_RequiresToken);
            }

            return new TokenCloudCredentials(
                Session.CurrentEnvironment.Subscription.Id,
                token.Token.AccessToken);
        }

        protected async Task<IDictionary<string, string>> LoadServiceConfig(Datacenter dc, Service service)
        {
            await Console.WriteInfoLine(Strings.AzureCommandBase_FetchingServiceConfig, service.Value);

            // Get creds
            var creds = await GetAzureCredentials();
            var ns = XNamespace.Get("http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration");

            // Connect to the Compute Management Client
            using (var client = CloudContext.Clients.CreateComputeManagementClient(creds))
            {
                // Download config for the deployment
                var result = await client.Deployments.GetBySlotAsync(service.Value, DeploymentSlot.Production);

                var parsed = XDocument.Parse(result.Configuration);
                return parsed.Descendants(ns + "Setting").ToDictionary(
                    x => x.Attribute("name").Value,
                    x => x.Attribute("value").Value,
                    StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
