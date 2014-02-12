using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Scheduler.Models;
using PowerArgs;
using DefaultValueAttribute = PowerArgs.DefaultValueAttribute;

namespace NuCmd.Commands.Scheduler
{
    [Description("Creates a scheduler job collection")]
    public class NewColCommand : SchedulerServiceCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgDescription("The name of the collection")]
        public string Name { get; set; }

        [ArgShortcut("p")]
        [DefaultValue("Free")]
        [ArgDescription("The scheduler pricing plan to use for the collection. See http://www.windowsazure.com/en-us/pricing/details/scheduler/ for pricing details.")]
        public JobCollectionPlan Plan { get; set; }

        [ArgShortcut("l")]
        [ArgDescription("A friendly label to apply to the collection")]
        public string Label { get; set; }

        [ArgShortcut("mo")]
        [ArgDescription("Maximum number of occurrences for a job?")]
        public int? MaxJobOccurrence { get; set; }

        [ArgShortcut("mj")]
        [ArgDescription("Maximum number of jobs")]
        public int? MaxJobCount { get; set; }
        
        [ArgShortcut("mrf")]
        [ArgDescription("Maximum recurrence frequency")]
        public JobCollectionRecurrenceFrequency? MaxRecurrenceFrequency { get; set; }

        [ArgShortcut("mri")]
        [ArgDescription("Maximum recurrence interval")]
        public int? MinRecurrenceInterval { get; set; }

        protected override async Task OnExecute(SubscriptionCloudCredentials credentials)
        {
            if((MaxRecurrenceFrequency.HasValue && !MinRecurrenceInterval.HasValue) ||
                (MinRecurrenceInterval.HasValue && !MaxRecurrenceFrequency.HasValue)) {
                await Console.WriteErrorLine(Strings.Scheduler_ColNewCommand_MaxRecurrenceIncomplete);
            }
            else {
                JobCollectionMaxRecurrence maxRecurrence = null;
                if(MaxRecurrenceFrequency != null) {
                    maxRecurrence = new JobCollectionMaxRecurrence()
                    {
                        Frequency = MaxRecurrenceFrequency.Value,
                        Interval = MinRecurrenceInterval.Value
                    };
                }

                using (var client = CloudContext.Clients.CreateSchedulerManagementClient(credentials))
                {
                    await Console.WriteInfoLine(Strings.Scheduler_ColNewCommand_CreatingCollection, Name, CloudService);
                    if (!WhatIf)
                    {
                        await client.JobCollections.CreateAsync(
                            CloudService,
                            Name,
                            new JobCollectionCreateParameters()
                            {
                                Label = Label,
                                IntrinsicSettings = new JobCollectionIntrinsicSettings()
                                {
                                    Plan = Plan,
                                    Quota = new JobCollectionQuota()
                                    {
                                        MaxJobCount = MaxJobCount,
                                        MaxJobOccurrence = MaxJobOccurrence,
                                        MaxRecurrence = maxRecurrence
                                    }
                                }
                            },
                            CancellationToken.None);
                    }
                    await Console.WriteInfoLine(Strings.Scheduler_ColNewCommand_CreatedCollection, Name, CloudService);
                }
            }
        }
    }
}
