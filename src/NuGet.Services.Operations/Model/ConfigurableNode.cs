using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Model
{
    public abstract class ConfigurableNode
    {
        protected ConfigurableNode ConfigParent { get; private set; }

        public IList<ConfigSetting> Configuration { get; private set; }

        public ConfigSetting this[string key]
        {
            get
            {
                return GetSetting(key, null);
            }
        }

        protected ConfigurableNode(ConfigurableNode configParent)
        {
            Configuration = new List<ConfigSetting>();
            ConfigParent = configParent;
        }

        public virtual ConfigSetting GetSetting(string key, string service)
        {
            ConfigSetting setting = Configuration.FirstOrDefault(c => 
                String.Equals(key, c.Name, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(service, c.Service, StringComparison.OrdinalIgnoreCase));
            if (ConfigParent != null)
            {
                setting = ConfigParent.GetSetting(key, service) ?? setting;
            }
            return setting;
        }

        public virtual IDictionary<string, ConfigSetting> GetMergedSettings(string service)
        {
            // Get the merged settings from my parent if any
            IDictionary<string, ConfigSetting> parent;
            if (ConfigParent != null)
            {
                parent = ConfigParent.GetMergedSettings(service);
            }
            else
            {
                parent = new Dictionary<string, ConfigSetting>();
            }

            // Overwrite any settings in there with my settings
            foreach (var setting in Configuration
                .Where(s =>
                    String.Equals(s.Service, service, StringComparison.OrdinalIgnoreCase) ||
                    String.IsNullOrEmpty(s.Service)))
            {
                parent[setting.Name] = setting;
            }

            return parent;
        }
    }
}
