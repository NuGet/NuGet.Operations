using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public class AppModel
    {
        public static readonly Version DefaultVersion = new Version(3, 0);

        public Version Version { get; private set; }

        public IList<DeploymentEnvironment> Environments { get; private set; }
        public IList<AzureSubscription> Subscriptions { get; private set; }

        public AppModel() : this(DefaultVersion, new List<DeploymentEnvironment>(), new List<AzureSubscription>())
        {
        }

        public AppModel(Version version, IEnumerable<DeploymentEnvironment> environments, IEnumerable<AzureSubscription> subscriptions)
        {
            Version = version;
            Environments = environments.ToList();
            Subscriptions = subscriptions.ToList();
        }
    }
}
