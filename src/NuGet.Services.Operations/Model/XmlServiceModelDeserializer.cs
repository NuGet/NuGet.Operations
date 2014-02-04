using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NuGet.Services.Operations.Model
{
    public static class XmlServiceModelDeserializer
    {
        public static AppModel LoadServiceModel(string fileName)
        {
            return LoadServiceModel(XDocument.Load(fileName).Root);
        }

        public static AppModel LoadServiceModel(TextReader file)
        {
            return LoadServiceModel(XDocument.Load(file).Root);
        }

        public static AppModel LoadServiceModel(XElement root)
        {
            var subs = LoadSubscriptions(root.Element("subscriptions"));

            return new AppModel(
                root.AttributeValueOrDefault("version", Version.Parse, AppModel.DefaultVersion),
                LoadEnvironments(root.Element("environments"), subs),
                subs);
        }

        private static IEnumerable<AzureSubscription> LoadSubscriptions(XElement root)
        {
            if (root == null)
            {
                return Enumerable.Empty<AzureSubscription>();
            }
            return root.Elements("subscription").Select(elem => new AzureSubscription()
            {
                Id = elem.AttributeValueOrDefault("id"),
                Name = elem.AttributeValueOrDefault("name")
            });
        }

        
        private static IEnumerable<DeploymentEnvironment> LoadEnvironments(XElement root, IEnumerable<AzureSubscription> subscriptions = null)
        {
            if (root == null)
            {
                return Enumerable.Empty<DeploymentEnvironment>();
            }

            subscriptions = subscriptions ?? Enumerable.Empty<AzureSubscription>();
            var subDict = subscriptions
                .ToDictionaryByFirstItemWithKey(
                    s => s.Name,
                    StringComparer.OrdinalIgnoreCase);

            return root.Elements("environment").Select(e => LoadEnvironment(e, subDict));
        }

        private static DeploymentEnvironment LoadEnvironment(XElement e, IDictionary<string, AzureSubscription> subscriptions)
        {
            var env = new DeploymentEnvironment()
            {
                Name = e.AttributeValueOrDefault("name"),
                Version = Version.Parse(e.AttributeValueOrDefault("version"))
            };
            string subName = e.AttributeValueOrDefault("subscription");
            if (!String.IsNullOrEmpty(subName))
            {
                AzureSubscription sub;
                if (subscriptions.TryGetValue(subName, out sub))
                {
                    env.Subscription = sub;
                }
                else
                {
                    env.Subscription = new AzureSubscription() { Name = subName };
                }
            }

            env.Datacenters.AddRange(e.Elements("datacenter").Select(el => LoadDatacenter(el)));
            return env;
        }

        private static Datacenter LoadDatacenter(XElement e)
        {
            var dc = new Datacenter()
            {
                Id = e.AttributeValueOrDefault<int>("id", Int32.Parse),
                Region = e.AttributeValueOrDefault("region"),
                AffinityGroup = e.AttributeValueOrDefault("affinityGroup")
            };

            var resElem = e.Element("resources");
            if (resElem != null)
            {
                dc.Resources.AddRange(resElem.Elements().Select(el => LoadResource(el)));
            }

            var svcElem = e.Element("services");
            if (svcElem != null)
            {
                dc.Services.AddRange(svcElem.Elements().Select(el => LoadService(el)));
            }
            return dc;
        }

        private static Resource LoadResource(XElement e)
        {
            return new Resource()
            {
                Name = e.AttributeValueOrDefault("name"),
                Type = e.Name.LocalName,
                Value = e.Value
            };
        }

        private static Service LoadService(XElement e)
        {
            return new Service()
            {
                Name = e.AttributeValueOrDefault("name"),
                Type = e.Name.LocalName,
                Value = e.Value,
                Url = e.AttributeValueOrDefault<Uri>("url", s => new Uri(s))
            };
        }
    }
}
