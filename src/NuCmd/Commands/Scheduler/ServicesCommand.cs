﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Scheduler;

namespace NuCmd.Commands.Scheduler
{
    [Description("Lists the scheduler services available")]
    public class ServicesCommand : AzureConnectionCommandBase
    {
        protected override async Task OnExecute(SubscriptionCloudCredentials credentials)
        {
            using (var client = CloudContext.Clients.CreateCloudServiceManagementClient(credentials))
            {
                await Console.WriteInfoLine(Strings.Scheduler_CsListCommand_ListingAvailableServices);
                var response = await client.CloudServices.ListAsync();
                await Console.WriteTable(response, r => new
                {
                    r.Name,
                    r.Label,
                    r.Description,
                    r.GeoRegion
                });
            }
        }
    }
}
