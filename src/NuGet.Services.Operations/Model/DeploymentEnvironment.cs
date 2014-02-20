using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Model
{
    public class DeploymentEnvironment
    {
        public string Name { get; set; }
        public AzureSubscription Subscription { get; set; }
        public Version Version { get; set; }

        public IList<PackageSource> PackageSources { get; private set; }
        public IList<SecretStoreReference> SecretStores { get; private set; }
        public IDictionary<string, string> Config { get; private set; }
        public IList<Datacenter> Datacenters { get; private set; }

        public AppModel App { get; private set; }

        public string FullName { get { return App.Name + "-" + Name; } }

        public Datacenter this[int id]
        {
            get
            {
                return Datacenters.FirstOrDefault(dc => dc.Id == id);
            }
        }

        public DeploymentEnvironment(AppModel app)
        {
            App = app;
            Datacenters = new List<Datacenter>();
            PackageSources = new List<PackageSource>();
            SecretStores = new List<SecretStoreReference>();
            Config = new Dictionary<string, string>();
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

        public Service GetService(int datacenter, string name)
        {
            Datacenter dc = this[datacenter];
            if (dc == null)
            {
                throw new KeyNotFoundException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DeploymentEnvironment_UnknownDatacenter,
                    datacenter));
            }
            return dc.GetService(name);
        }
    }
}
