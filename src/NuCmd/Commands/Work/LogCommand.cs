using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Newtonsoft.Json;
using NuGet.Services.Client;
using NuGet.Services.Models;
using PowerArgs;

namespace NuCmd.Commands.Work
{
    public class LogCommand : WorkServiceCommandBase
    {
        [ArgShortcut("i")]
        [ArgPosition(0)]
        [ArgDescription("The ID of the invocation to get the log for, or a Job Name to get the log for the latest invocation of that job")]
        public string IdOrJob { get; set; }

        protected override async Task OnExecute()
        {
            var client = await OpenClient();
            if (client == null) { return; }

            // Try to parse the ID as a GUID
            Guid _;
            string id = IdOrJob;
            if (!Guid.TryParse(IdOrJob, out _))
            {
                await Console.WriteInfoLine(Strings.Work_LogCommand_FetchingLatestInvocation, IdOrJob);
                var invocationResponse = await client.Jobs.GetLatestInvocation(IdOrJob);
                if (!await ReportHttpStatus(invocationResponse))
                {
                    return;
                }
                var invocation = await invocationResponse.ReadContent();
                id = invocation.Id.ToString("N");
            }
            await Console.WriteInfoLine(Strings.Work_LogCommand_FetchingLog, id);
            var logResponse = await client.Invocations.GetLog(id);
            
            if (await ReportHttpStatus(logResponse))
            {
                var log = await logResponse.ReadContent();
                var events = LogEvent.ParseLogEvents(log);
                string message = String.Format(Strings.Work_LogCommand_RenderingLog, id);
                await Console.WriteInfoLine(message);
                await Console.WriteInfoLine(new String('-', message.Length));
                foreach (var evt in events)
                {
                    await WriteEvent(evt);
                }
                message = String.Format(Strings.Work_LogCommand_RenderedLog, id);
                await Console.WriteInfoLine(new String('-', message.Length));
                await Console.WriteInfoLine(message);
            }
        }

        private async Task WriteEvent(LogEvent evt)
        {
            string message = evt.Message;
            switch (evt.Level)
            {
                case LogEventLevel.Critical:
                    await Console.WriteFatalLine(message);
                    break;
                case LogEventLevel.Error:
                    await Console.WriteErrorLine(message);
                    break;
                case LogEventLevel.Informational:
                    await Console.WriteInfoLine(message);
                    break;
                case LogEventLevel.Verbose:
                    await Console.WriteTraceLine(message);
                    break;
                case LogEventLevel.Warning:
                    await Console.WriteWarningLine(message);
                    break;
            }
        }
    }
}
