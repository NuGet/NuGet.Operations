using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public class Service : ConfigurableNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public Uri Uri { get; set; }

        public Datacenter Datacenter { get; private set; }

        public Service(Datacenter dc) : base(dc)
        {
            Datacenter = dc;
        }

        public IDictionary<string, ConfigSetting> GetMergedSettings()
        {
            return GetMergedSettings(Name);
        }
    }
}
