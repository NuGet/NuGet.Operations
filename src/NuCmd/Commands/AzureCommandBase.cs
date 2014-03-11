using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute.Models;
using NuGet.Services.Operations;
using NuGet.Services.Operations.Model;

namespace NuCmd.Commands
{
    public abstract class AzureCommandBase : EnvironmentCommandBase
    {
        protected async Task<SubscriptionCloudCredentials> GetCredentials()
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
            var creds = await GetCredentials();
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

    public abstract class AzureConnectionCommandBase : AzureCommandBase
    {
        protected override async Task OnExecute()
        {
            var cred = await GetCredentials();
            bool expired = false;
            try
            {
                await OnExecute(cred);
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
            }
        }

        protected abstract Task OnExecute(SubscriptionCloudCredentials credentials);
    }
}
