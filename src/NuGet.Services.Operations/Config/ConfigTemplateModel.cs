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
        public dynamic Services { get; set; }

        public ConfigTemplateModel(SecretStore secrets, Service service)
        {
            Resources = new ConfigObject(
                service
                    .Datacenter
                    .Resources
                    .GroupBy(r => r.Type)
                    .ToDictionary(g => g.Key, g => (object)g.ToDictionary(r => r.Name, r => ResolveValue(secrets, service, r))));
            Service = service;
            Services = new ConfigObject(
                service
                    .Datacenter
                    .Services
                    .ToDictionary(s => s.Name, s => (object)new ServiceModel(s, secrets)));
        }

        private object ResolveValue(SecretStore secrets, Service service, Resource r)
        {
            // Simpler to just to sync here.
            return ResourceResolver.Resolve(secrets, service, r);
        }
    }
}
