using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuCmd.Commands
{
    [Description("Displays information about the current environment")]
    public class EnvCommand : Command
    {
        protected override async Task OnExecute()
        {
            if (Session == null)
            {
                await Console.WriteInfoLine(Strings.EnvCommand_NoSession);
            }
            else if (Session.CurrentEnvironment == null)
            {
                await Console.WriteInfoLine(Strings.EnvCommand_NoEnv);
            }
            else
            {
                await Console.WriteInfoLine(Strings.EnvCommand_Data_Env, Session.CurrentEnvironment.Name);
                await Console.WriteInfoLine(Strings.EnvCommand_Data_Sub, 
                    Session.CurrentEnvironment.Subscription == null ? String.Empty : Session.CurrentEnvironment.Subscription.Name,
                    Session.CurrentEnvironment.Subscription == null ? String.Empty : Session.CurrentEnvironment.Subscription.Id);
                await Console.WriteInfoLine(Strings.EnvCommand_Data_Cert,
                    Session.CurrentEnvironment.Subscription == null ? String.Empty : (
                        Session.CurrentEnvironment.Subscription.Certificate == null ? String.Empty : Session.CurrentEnvironment.Subscription.Certificate.Thumbprint));
                await Console.WriteInfoLine(Strings.EnvCommand_Data_Datacenters);
                foreach (var dc in Session.CurrentEnvironment.Datacenters)
                {
                    await Console.WriteInfoLine(Strings.EnvCommand_Data_Datacenter, dc.Id, dc.Region);
                    foreach (var service in dc.Services)
                    {
                        await Console.WriteInfoLine(Strings.EnvCommand_Data_Datacenter_Service, service.Name, service.Uri == null ? String.Empty : service.Uri.AbsoluteUri);
                    }
                    await Console.WriteInfoLine();
                    foreach (var resource in dc.Resources)
                    {
                        await Console.WriteInfoLine(Strings.EnvCommand_Data_Datacenter_Resource, resource.Type, resource.Name, resource.Value);
                    }
                }
            }
        }
    }
}
