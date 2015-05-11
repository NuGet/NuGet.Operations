// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Scheduler;
using Microsoft.WindowsAzure.Scheduler.Models;
using PowerArgs;

namespace NuCmd.Commands.Scheduler
{
    [Description("Lists the jobs in the specified collection")]
    public class JobsCommand : JobCollectionCommandBase
    {
        [ArgShortcut("j")]
        [ArgPosition(0)]
        [ArgDescription("Specify this value to retrieve information on a specific job.")]
        public string Id { get; set; }

        protected override async Task OnExecute(SubscriptionCloudCredentials credentials)
        {
            using (var client = CloudContext.Clients.CreateSchedulerClient(credentials, CloudService, Collection))
            {
                await Console.WriteInfoLine(Strings.Scheduler_JobsCommand_ListingJobs, CloudService, Collection);
                if (String.IsNullOrEmpty(Id))
                {
                    var jobs = await client.Jobs.ListAsync(new JobListParameters(), CancellationToken.None);
                    await Console.WriteTable(jobs, r => new
                        {
                            r.Id,
                            r.State,
                            r.Status
                        });
                }
                else
                {
                    var job = await client.Jobs.GetAsync(Id, CancellationToken.None);
                    await Console.WriteObject(job.Job);
                }
            }
        }
    }
}
