﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Scheduler.Models;
using NuGet.Services.Client;
using NuGet.Services.Work.Models;
using PowerArgs;

namespace NuCmd.Commands.Scheduler
{
    [Description("Refreshes the URL used by the job to ping the job service")]
    public class RefreshJobCommand : JobCollectionCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("i")]
        [ArgDescription("The name of the job instance to create")]
        public string InstanceName { get; set; }

        [ArgShortcut("url")]
        [ArgDescription("The URI to the root of the work service")]
        public Uri ServiceUri { get; set; }

        protected override async Task OnExecute(SubscriptionCloudCredentials credentials)
        {
            if (ServiceUri == null)
            {
                await Console.WriteErrorLine(Strings.ParameterRequired, "SerivceUri");
            }
            else
            {
                using (var client = CloudContext.Clients.CreateSchedulerClient(credentials, CloudService, Collection))
                {
                    var job = await client.Jobs.GetAsync(InstanceName, CancellationToken.None);
                    if (job == null)
                    {
                        await Console.WriteErrorLine(Strings.Scheduler_RefreshJobCommand_NoSuchJob, InstanceName);
                    }
                    else if (job.Job.Action.Type == JobActionType.StorageQueue || job.Job.Action.Request == null)
                    {
                        await Console.WriteErrorLine(Strings.Scheduler_RefreshJobCommand_NotAWorkServiceJob, InstanceName);
                    }
                    else
                    {
                        Uri old = job.Job.Action.Request.Uri;
                        job.Job.Action.Request.Uri = new Uri(ServiceUri, "work/invocations");
                        await Console.WriteInfoLine(
                            Strings.Scheduler_RefreshJobCommand_UpdatingUrl,
                            InstanceName,
                            old.AbsoluteUri,
                            job.Job.Action.Request.Uri.AbsoluteUri);
                        if (!WhatIf)
                        {
                            await client.Jobs.CreateOrUpdateAsync(InstanceName, new JobCreateOrUpdateParameters()
                            {
                                StartTime = job.Job.StartTime,
                                Action = job.Job.Action,
                                Recurrence = job.Job.Recurrence
                            }, CancellationToken.None);
                        }
                    }
                }
            }
        }

        protected override Task LoadDefaultsFromContext()
        {
            if (Session != null && Session.CurrentEnvironment != null)
            {
                ServiceUri = ServiceUri ?? Session.CurrentEnvironment.GetServiceUri(datacenter: 0, service: "work");
            }
            return base.LoadDefaultsFromContext();
        }
    }
}
