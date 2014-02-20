using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using NuGet.Services.Operations.Secrets;

namespace NuGet.Services.Operations.Model
{
    public class Datacenter : ConfigurableNode
    {
        public int Id { get; set; }
        public string Region { get; set; }
        public string AffinityGroup { get; set; }

        public IList<Resource> Resources { get; private set; }
        public IList<Service> Services { get; private set; }

        public DeploymentEnvironment Environment { get; private set; }
        public string FullName { get { return Environment.FullName + "-" + Id.ToString(); } }

        public Datacenter(DeploymentEnvironment environment) : base(environment)
        {
            Resources = new List<Resource>();
            Services = new List<Service>();

            Environment = environment;
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

        public Resource FindResource(string type, string name)
        {
            return Resources.FirstOrDefault(r =>
                String.Equals(type, r.Type, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(name, r.Name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
