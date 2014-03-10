using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Services.Operations.Model;

namespace NuGet.Services.Operations.Config
{
    public abstract class ConfigTemplateSource
    {
        public abstract XDocument ReadConfigTemplate(Service service);
    }
}
