using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Scheduler;

namespace NuCmd.Commands.Scheduler
{
    public abstract class SchedulerCommandBase : Command
    {
        protected SubscriptionCloudCredentials Credentials { get; set; }

        protected override Task LoadDefaultsFromContext()
        {
            if (Session == null || 
                Session.CurrentEnvironment == null || 
                Session.CurrentEnvironment.Subscription == null)
            {
                throw new InvalidOperationException(Strings.Command_RequiresManagementCert);
            }
            Session.CurrentEnvironment.Subscription.ResolveCertificate();
            if (Session.CurrentEnvironment.Subscription.Certificate == null)
            {
                throw new InvalidOperationException(Strings.Command_RequiresManagementCert);
            }
            Credentials = new CertificateCloudCredentials(
                Session.CurrentEnvironment.Subscription.Id,
                Session.CurrentEnvironment.Subscription.Certificate);
            return Task.FromResult(true);
        }
    }
}
