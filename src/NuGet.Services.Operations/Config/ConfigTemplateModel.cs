using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using NuGet.Services.Operations.Model;
using NuGet.Services.Operations.Secrets;

namespace NuGet.Services.Operations.Config
{
    public class ConfigTemplateModel
    {
        public dynamic Resources { get; set; }
        public Service Service { get; set; }

        public ConfigTemplateModel(SecretStore secrets, Service service)
        {
            Resources = Expandoify(
                service
                    .Datacenter
                    .Resources
                    .GroupBy(r => r.Type)
                    .ToDictionary(g => g.Key, g => (object)g.ToDictionary(r => r.Name, r => ResolveValue(secrets, service, r))));
            Service = service;
        }

        private static ExpandoObject Expandoify(IDictionary<string, object> dict)
        {
            var expando = new ExpandoObject();
            var dest = (IDictionary<string, object>)expando;

            foreach (var pair in dict)
            {
                dest.Add(pair.Key, ConvertValue(pair.Value));
            }

            return expando;
        }

        private static object ConvertValue(object input)
        {
            IDictionary<string, object> nested = input as IDictionary<string, object>;
            if (nested != null)
            {
                return Expandoify(nested);
            }
            else if(!(input is string))
            {
                // Gotta shortcut string, because it is IEnumerable but we don't want it to be treated as such
                IEnumerable enumer = input as IEnumerable;
                if (enumer != null)
                {
                    return enumer.Cast<object>().Select(ConvertValue);
                }
            }
            return input;
        }

        private object ResolveValue(SecretStore secrets, Service service, Resource r)
        {
            // Simpler to just to sync here.
            return ResourceResolver.Resolve(secrets, service, r);
        }
    }
}
