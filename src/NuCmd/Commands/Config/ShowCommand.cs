using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands.Config
{
    [Description("Displays the aggregate configuration for the specified Environment and Datacenter")]
    public class ShowCommand : Command
    {
        [ArgShortcut("e")]
        [ArgDescription("The environment to display configuration for (defaults to the current environment)")]
        public string Environment { get; set; }

        [ArgShortcut("dc")]
        [ArgDescription("The datacenter to display configuration for (shows environment-global config if not present)")]
        public int? Datacenter { get; set; }

        protected override async Task OnExecute()
        {
            // Load the current environment and datacenter
            var env = GetEnvironment(Environment);

            IDictionary<string, string> config;
            string name = env.Name;
            if(Datacenter.HasValue) {
                var dc = GetDatacenter(env, Datacenter.Value);
            
                // Resolve config
                config = dc.MergeConfig(env);
                name = env.Name + "-" + Datacenter.Value;
            } else {
                config = env.Config;
            }

            // Write config to console
            await Console.WriteInfoLine(String.Format(
                CultureInfo.CurrentCulture,
                Strings.Config_ShowCommand_Header,
                name));
            if (config.Count > 0)
            {
                int maxKey = config.Keys.Max(s => s.Length);
                foreach (var pair in config)
                {
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Config_ShowCommand_Row,
                        pair.Key.PadRight(maxKey + 1),
                        pair.Value));
                }
            }
        }
    }
}
