using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Reactive.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using NuGet.Services.Work;
using PowerArgs;
using System.Reactive;

namespace NuCmd.Commands.Work
{
    [Description("DECOMMISSIONED. Use 'jobhost.exe' in the NuGet.Services.Work repo instead")]
    public class RunCommand : Command
    {
        protected override async Task OnExecute()
        {
            await Console.WriteErrorLine("DECOMMISSIONED. Use 'jobhost.exe' in the NuGet.Services.Work repo instead");
        }
    }
}
