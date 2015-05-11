// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands
{
    [Description("Displays information about the current environment")]
    public class EnvCommand : Command
    {
        [ArgDescription("The environment to display information for (defaults to the current environment)")]
        public string Environment { get; set; }
     
        protected override async Task OnExecute()
        {
            var environment = GetEnvironment(Environment);

            await Console.WriteInfoLine(Strings.EnvCommand_Data_Env, environment.Name);
            await Console.WriteInfoLine(Strings.EnvCommand_Data_Sub, 
                environment.Subscription == null ? String.Empty : environment.Subscription.Name,
                environment.Subscription == null ? String.Empty : environment.Subscription.Id);
            await Console.WriteInfoLine(Strings.EnvCommand_Data_Cert,
                environment.Subscription == null ? String.Empty : (
                    environment.Subscription.Certificate == null ? String.Empty : environment.Subscription.Certificate.Thumbprint));
            
            foreach (var source in environment.PackageSources)
            {
                await Console.WriteInfoLine(Strings.EnvCommand_Data_PackageSource, source.Type, source.Name, source.Value);
            }

            if (environment.SecretStore != null)
            {
                await Console.WriteInfoLine(
                    Strings.EnvCommand_Data_SecretStore, 
                    environment.SecretStore.Type,
                    environment.SecretStore.Value);
            }

            if (environment.ConfigTemplates != null)
            {
                await Console.WriteInfoLine(
                    Strings.EnvCommand_Data_ConfigTemplates,
                    environment.ConfigTemplates.Type,
                    environment.ConfigTemplates.Value);
            }

            await Console.WriteInfoLine(Strings.EnvCommand_Data_Datacenters);

            foreach (var dc in environment.Datacenters)
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
