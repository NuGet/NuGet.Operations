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
    [Description("Deletes the specified scheduler job collection")]
    public class DeleteColCommand : SchedulerServiceCommandBase
    {
        [ArgRequired]
        [ArgShortcut("c")]
        [ArgPosition(0)]
        [ArgDescription("The name of the collection to delete")]
        public string Name { get; set; }

        protected override async Task OnExecute(SubscriptionCloudCredentials credentials)
        {
            using (var client = CloudContext.Clients.CreateSchedulerManagementClient(credentials))
            {
                await Console.WriteInfoLine(Strings.Scheduler_ColDeleteCommand_DeletingCollection, CloudService, Name);
                await client.JobCollections.DeleteAsync(CloudService, Name);
            }
        }
    }
}
