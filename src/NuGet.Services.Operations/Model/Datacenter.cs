using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public class Datacenter
    {
        public int Id { get; set; }
        public string Region { get; set; }
        public string AffinityGroup { get; set; }

        public IList<Resource> Resources { get; private set; }
        public IList<Service> Services { get; private set; }

        public Datacenter()
        {
            Resources = new List<Resource>();
            Services = new List<Service>();
        }

        public Uri GetServiceUri(string service)
        {
            Service svc = GetService(service);
            if (svc == null)
            {
                return null;
            }
            return svc.Uri;
        }

        public Service GetService(string service)
        {
            return Services.FirstOrDefault(s => String.Equals(s.Name, service, StringComparison.OrdinalIgnoreCase));
        }

        public Resource GetResource(string resource)
        {
            return Resources.FirstOrDefault(r => String.Equals(r.Name, resource, StringComparison.OrdinalIgnoreCase));
        }
    }
}
