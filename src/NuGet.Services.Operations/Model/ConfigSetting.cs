using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Model
{
    public class ConfigSetting
    {
        public string Name { get; set; }
        public ConfigSettingType Type { get; set;}
        public string Service { get; set; }
        public string Value { get; set; }
    }

    public enum ConfigSettingType
    {
        Literal,
        Resource
    }
}
