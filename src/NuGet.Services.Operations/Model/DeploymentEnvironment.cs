using System;
using System.Collections.Generic;
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
                return Datacenters.First(dc => dc.Id == id);
            }
        }

        public DeploymentEnvironment()
        {
            Datacenters = new List<Datacenter>();
        }
    }
}
