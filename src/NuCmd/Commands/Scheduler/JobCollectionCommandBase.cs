using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands.Scheduler
{
    public abstract class JobCollectionCommandBase : SchedulerServiceCommandBase
    {
        [ArgPosition(1)]
        [ArgShortcut("c")]
        [ArgDescription("The collection to operate within")]
        public string Collection { get; set; }
        
        protected override async Task LoadDefaultsFromContext()
        {
            await base.LoadDefaultsFromContext();

            if (Session != null && Session.CurrentEnvironment != null)
            {
                Collection = String.IsNullOrEmpty(Collection) ?
                    String.Format("nuget-{0}-0-scheduler", Session.CurrentEnvironment.Name) :
                    Collection;
            }
        }
    }
}
