// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Scheduler;
using NuCmd.Commands.Azure;
using PowerArgs;

namespace NuCmd.Commands.Scheduler
{
    [Description("Lists the scheduler job collections available")]
    public class CollectionsCommand : SchedulerServiceCommandBase
    {
        [ArgShortcut("c")]
        [ArgPosition(0)]
        [ArgDescription("The name of a specific job collection to view information about")]
        public string Name { get; set; }

        protected override Task OnExecute(SubscriptionCloudCredentials credentials)
        {
            if (String.IsNullOrEmpty(Name))
            {
                return GetAllCollections(credentials);
            }
            else
            {
                return GetSingleCollection(credentials);
            }
        }

        private async Task GetSingleCollection(SubscriptionCloudCredentials credentials)
        {
            using (var client = CloudContext.Clients.CreateSchedulerManagementClient(credentials))
            {
                await Console.WriteInfoLine(Strings.Scheduler_CollectionsCommand_GettingCollection, Name, CloudService);
                var response = await client.JobCollections.GetAsync(CloudService, Name);
                await Console.WriteObject(response);
            }
        }

        private async Task GetAllCollections(SubscriptionCloudCredentials credentials)
        {
            using (var client = CloudContext.Clients.CreateCloudServiceManagementClient(credentials))
            {
                await Console.WriteInfoLine(Strings.Scheduler_CollectionsCommand_ListingCollections, CloudService);
                var response = await client.CloudServices.GetAsync(CloudService);
                await Console.WriteTable(response.Resources.Where(r =>
                    String.Equals(r.ResourceProviderNamespace, "scheduler", StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(r.Type, "jobcollections", StringComparison.OrdinalIgnoreCase)),
                    r => new
                    {
                        r.Name,
                        r.State,
                        r.SubState,
                        r.Plan,
                        r.OutputItems
                    });
            }
        }
    }
}
