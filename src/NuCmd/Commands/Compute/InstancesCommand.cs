// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute.Models;
using PowerArgs;

namespace NuCmd.Commands.Compute
{
    public class InstancesCommand : EnvironmentCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("s")]
        [ArgDescription("The name of the cloud service/VM to list instances of")]
        public string Service { get; set; }

        protected override async Task OnExecute()
        {
            // Get cloud service info for the machine
            using(var compute = CloudContext.Clients.CreateComputeManagementClient(await GetAzureCredentials()))
            {
                var slot = await compute.Deployments.GetBySlotAsync(Service, DeploymentSlot.Production);
                await Console.WriteTable(slot.RoleInstances, r => new
                {
                    r.HostName,
                    r.InstanceName,
                    r.RoleName,
                    r.IPAddress
                });
            }
        }
    }
}
