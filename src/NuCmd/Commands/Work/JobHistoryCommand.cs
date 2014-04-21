using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Work;
using NuGet.Services.Work.Models;
using PowerArgs;

namespace NuCmd.Commands.Work
{
    [Description("Lists invocations for a particular job")]
    public class JobHistoryCommand : WorkServiceCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("-j")]
        [ArgDescription("The job to get the history for")]
        public string Job { get; set; }

        [ArgShortcut("-l")]
        [ArgDescription("The maximum number of entries to retrieve")]
        public int? Limit { get; set; }

        [ArgShortcut("-a")]
        [ArgDescription("Set this switch to return all instead of the default of filtering to 10")]
        public bool All { get; set; }

        protected override async Task OnExecute()
        {
            var client = await OpenClient();
            if (client == null) { return; }

            var response = await client.Jobs.GetByJob(Job, null, null, All ? Limit : (Limit ?? 10));

            if (await ReportHttpStatus(response))
            {
                var instances = await response.ReadContent();
                await Console.WriteTable(
                    instances, i => new
                    {
                        i.Id,
                        i.JobInstanceName,
                        i.Status,
                        i.Result,
                        i.QueuedAt,
                        i.UpdatedAt,
                        i.Job
                    });
            }
        }
    }
}
