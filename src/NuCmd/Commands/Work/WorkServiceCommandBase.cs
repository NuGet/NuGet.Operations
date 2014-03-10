using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services;
using NuGet.Services.Work.Client;
using PowerArgs;

namespace NuCmd.Commands.Work
{
    public abstract class WorkServiceCommandBase : ServiceCommandBase<WorkClient>
    {
        protected WorkServiceCommandBase() : base("work", c => new WorkClient(c)) { }
    }
}
