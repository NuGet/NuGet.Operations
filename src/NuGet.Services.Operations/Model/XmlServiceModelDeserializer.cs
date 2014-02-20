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

            var app = new AppModel(
                root.AttributeValueOrDefault("name"),
                root.AttributeValueOrDefault("version", Version.Parse, AppModel.DefaultVersion));

            app.Environments.AddRange(LoadEnvironments(root.Element("environments"), app, subs));
            app.Subscriptions.AddRange(subs);
            return app;
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

        
        private static IEnumerable<DeploymentEnvironment> LoadEnvironments(XElement root, AppModel app, IEnumerable<AzureSubscription> subscriptions = null)
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

            return root.Elements("environment").Select(e => LoadEnvironment(e, app, subDict));
        }

        private static DeploymentEnvironment LoadEnvironment(XElement e, AppModel app, IDictionary<string, AzureSubscription> subscriptions)
        {
            var env = new DeploymentEnvironment(app)
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

            LoadConfig(env.Config, e.Element("config"));

            env.Datacenters.AddRange(e.Elements("datacenter").Select(el => LoadDatacenter(el, env)));

            var srcElem = e.Element("packageSources");
            if (srcElem != null)
            {
                env.PackageSources.AddRange(srcElem.Elements().Select(el => LoadComponent<PackageSource>(el)));
            }

            var secElem = e.Element("secretStores");
            if (secElem != null)
            {
                env.SecretStores.AddRange(secElem.Elements().Select(el => LoadComponent<SecretStoreReference>(el)));
            }

            return env;
        }

        private static Datacenter LoadDatacenter(XElement e, DeploymentEnvironment env)
        {
            var dc = new Datacenter(env)
            {
                Id = e.AttributeValueOrDefault<int>("id", Int32.Parse),
                Region = e.AttributeValueOrDefault("region"),
                AffinityGroup = e.AttributeValueOrDefault("affinityGroup")
            };

            var resElem = e.Element("resources");
            if (resElem != null)
            {
                dc.Resources.AddRange(resElem.Elements().Select(el => LoadComponent<Resource>(el)));
            }

            var svcElem = e.Element("services");
            if (svcElem != null)
            {
                dc.Services.AddRange(svcElem.Elements().Select(el => LoadService(el)));
            }

            LoadConfig(dc.Config, e.Element("config"));

            return dc;
        }

        private static Service LoadService(XElement e)
        {
            return LoadComponent(e, new Service()
            {
                Uri = e.AttributeValueOrDefault<Uri>("url", s => new Uri(s))
            });
        }

        private static T LoadComponent<T>(XElement e) where T : EnvironmentComponentBase, new()
        {
            return LoadComponent(e, new T());
        }

        private static T LoadComponent<T>(XElement e, T instance) where T : EnvironmentComponentBase
        {
            instance.Name = e.AttributeValueOrDefault("name");
            instance.Type = e.Name.LocalName;
            instance.Value = e.Value;
            instance.Version = e.AttributeValueOrDefault<Version>(
                "version", Version.Parse, new Version(1, 0));

            foreach (var attr in e.Attributes())
            {
                instance.Attributes[attr.Name.LocalName] = attr.Value;
            }

            return instance;
        }

        private static void LoadConfig(IDictionary<string, string> dictionary, XElement configElement)
        {
            if (configElement != null)
            {
                foreach (var setting in configElement.Elements("setting"))
                {
                    var attr = setting.Attribute("name");
                    if (attr == null)
                    {
                        throw new InvalidDataException(Strings.XmlServiceModelDeserializer_SettingMissingName);
                    }
                    dictionary[attr.Value] = setting.Value;
                }
            }
        }
    }
}
