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
    public abstract class AzureConnectionCommandBase : EnvironmentCommandBase
    {
        protected override async Task OnExecute()
        {
            var cred = await GetAzureCredentials();
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
