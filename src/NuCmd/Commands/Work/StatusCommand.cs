using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Work.Models;
using PowerArgs;

namespace NuCmd.Commands.Work
{
    [Description("Gets the status of each job in the work service")]
    public class StatusCommand : WorkServiceCommandBase
    {
        protected override async Task OnExecute()
        {
            var client = await OpenClient();
            if (client == null) { return; }

            var response = await client.Invocations.GetStatus();

            if (await ReportHttpStatus(response))
            {
                await Console.WriteTable(await response.ReadContent(), i => new
                {
                    i.Job,
                    i.JobInstanceName,
                    i.Status,
                    i.Result,
                    i.QueuedAt,
                    i.UpdatedAt,
                    i.Id
                });
            }
        }
    }
}