using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public abstract class EnvironmentComponentBase
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        
        public IDictionary<string, string> Attributes { get; protected set; }

        protected EnvironmentComponentBase()
        {
            Attributes = new Dictionary<string, string>();
        }
    }
}
