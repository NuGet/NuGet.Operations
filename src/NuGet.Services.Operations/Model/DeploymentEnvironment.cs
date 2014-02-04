using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public class DeploymentEnvironment
    {
        public string Name { get; set; }
        public AzureSubscription Subscription { get; set; }
        public Version Version { get; set; }
        
        public IList<Datacenter> Datacenters { get; private set; }

        public Datacenter this[int id]
        {
            get
            {
                return Datacenters.FirstOrDefault(dc => dc.Id == id);
            }
        }

        public DeploymentEnvironment()
        {
            Datacenters = new List<Datacenter>();
        }

        public Uri GetServiceUri(int datacenter, string service)
        {
            Datacenter dc = this[datacenter];
            if (dc == null)
            {
                throw new KeyNotFoundException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DeploymentEnvironment_UnknownDatacenter,
                    datacenter));
            }
            return dc.GetServiceUri(service);
        }
    }
}
