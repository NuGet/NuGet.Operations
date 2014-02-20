using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands.Config
{
    [Description("Displays config settings for the specified service")]
    public class ShowCommand : DatacenterCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("s")]
        [ArgDescription("The service to display configuration for")]
        public string Service { get; set; }

        protected override async Task OnExecute()
        {
            var dc = GetDatacenter();
            var service = dc.GetService(Service);
            if (service == null)
            {
                await Console.WriteErrorLine(Strings.Config_ShowCommand_NoSuchService, Service, dc.FullName);
            }
            else
            {
                await Console.WriteInfoLine(Strings.Config_ShowCommand_DisplayingConfig, Service, dc.FullName);
                foreach (var setting in service.GetMergedSettings().Values)
                {
                    await Console.WriteInfoLine(Strings.Config_ShowCommand_ConfigEntry, setting.Name, setting.Value, setting.Type);
                }
            }
        }
    }
}
