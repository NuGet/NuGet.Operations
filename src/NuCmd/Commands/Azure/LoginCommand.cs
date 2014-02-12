using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Models;
using Microsoft.WindowsAzure.Subscriptions;

namespace NuCmd.Commands.Azure
{
    public class LoginCommand : EnvironmentCommandBase
    {
        protected override async Task OnExecute()
        {
            var env = GetEnvironment();
            var token = await Session.AzureTokens.Authenticate(env.Subscription.Id);
            if (token == null)
            {
                await Console.WriteErrorLine(Strings.Azure_LoginCommand_AuthenticationFailed);
            }
            else
            {
                await Console.WriteInfoLine(Strings.Azure_LoginCommand_AuthenticatedGettingSubscription);

                SubscriptionGetResponse resp;
                using (var client = CloudContext.Clients.CreateManagementClient(new TokenCloudCredentials(env.Subscription.Id, token.Token.AccessToken)))
                {
                    resp = await client.Subscriptions.GetAsync(CancellationToken.None);
                }
                if (resp.SubscriptionStatus == SubscriptionStatus.Active)
                {
                    await Console.WriteInfoLine(Strings.Azure_LoginCommand_AuthenticationComplete);
                    await Session.AzureTokens.StoreToken(token);
                }
                else
                {
                    await Console.WriteErrorLine(Strings.Azure_LoginCommand_SubscriptionDisabled);
                }
            }
        }
    }
}
