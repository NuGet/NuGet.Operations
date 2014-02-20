using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public abstract class ModelComponentBase
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public Version Version { get; set; }

        public IDictionary<string, string> Attributes { get; protected set; }

        protected ModelComponentBase()
        {
            Attributes = new Dictionary<string, string>();
        }
    }

    public abstract class NamedModelComponentBase : ModelComponentBase
    {
        public string Name { get; set; }
    }
}
