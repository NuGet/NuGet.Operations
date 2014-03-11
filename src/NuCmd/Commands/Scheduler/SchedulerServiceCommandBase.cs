using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands.Scheduler
{
    public abstract class SchedulerServiceCommandBase : AzureConnectionCommandBase
    {
        [ArgShortcut("cs")]
        [ArgDescription("Specifies the scheduler service to work with. Defaults to the standard one for this environment (nuget-[environment]-scheduler)")]
        public string CloudService { get; set; }

        [ArgShortcut("dc")]
        [ArgDescription("Specifies the datacenter of the work service to interact with.")]
        public int? Datacenter { get; set; }

        protected override async Task LoadDefaultsFromContext()
        {
            await base.LoadDefaultsFromContext();

            if (Session != null && Session.CurrentEnvironment != null)
            {
                CloudService = String.IsNullOrEmpty(CloudService) ?
                    String.Format("nuget-{0}-scheduler", Session.CurrentEnvironment.Name) :
                    CloudService;
            }
        }
    }
}
