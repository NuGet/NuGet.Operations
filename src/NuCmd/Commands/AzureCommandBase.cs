using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using NuGet.Services.Operations;

namespace NuCmd.Commands
{
    public abstract class AzureCommandBase : EnvironmentCommandBase
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

        private async Task<SubscriptionCloudCredentials> GetCredentials()
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
    }
}
