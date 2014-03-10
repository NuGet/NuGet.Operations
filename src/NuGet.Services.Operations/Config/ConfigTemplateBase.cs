using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations.Model;
using RazorEngine.Templating;

namespace NuGet.Services.Operations.Config
{
    public class ConfigTemplateBase : TemplateBase<ConfigTemplateModel>
    {
        public dynamic resources { get { return Model.Resources; } }
        public Service service { get { return Model.Service; } }
    }
}
