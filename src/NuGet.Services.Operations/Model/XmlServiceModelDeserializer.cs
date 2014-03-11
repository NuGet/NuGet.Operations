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
        public static AppModel LoadAppModel(string fileName)
        {
            return LoadAppModel(XDocument.Load(fileName).Root);
        }

        public static AppModel LoadAppModel(TextReader file)
        {
            return LoadAppModel(XDocument.Load(file).Root);
        }

        public static AppModel LoadAppModel(XElement root)
        {
            var subs = LoadSubscriptions(root.Element("subscriptions"));

            var app = new AppModel(
                root.AttributeValueOrDefault("name"),
                root.AttributeValueOrDefault("version", Version.Parse, AppModel.DefaultVersion));

            app.Resources.AddRange(LoadComponents<Resource>(root.Element("resources")));

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

            env.Datacenters.AddRange(e.Elements("datacenter").Select(el => LoadDatacenter(el, env)));
            env.PackageSources.AddRange(LoadComponents<PackageSource>(e.Element("packageSources")));

            var secElem = e.Element("secretStore");
            if (secElem != null)
            {
                env.SecretStore = LoadComponent<SecretStoreReference>(secElem, typeFromAttribute: true);
            }

            var tmplElem = e.Element("configTemplates");
            if (tmplElem != null)
            {
                env.ConfigTemplates = LoadComponent<ConfigTemplateReference>(tmplElem, typeFromAttribute: true);
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

            dc.Resources.AddRange(LoadComponents<Resource>(e.Element("resources")));
            dc.Services.AddRange(LoadServices(dc, e.Element("services")));

            return dc;
        }

        private static IEnumerable<Service> LoadServices(Datacenter dc, XElement root)
        {
            if (root == null)
            {
                return Enumerable.Empty<Service>();
            }
            return root.Elements().Select(e =>
            {
                var svc = new Service(dc);
                LoadComponent(e, typeFromAttribute: false, instance: svc);
                svc.Uri = e.AttributeValueOrDefault<Uri>("url", s => new Uri(s));
                return svc;
            });
        }

        private static IEnumerable<T> LoadComponents<T>(XElement root) where T : ModelComponentBase, new()
        {
            return LoadComponents<T>(root, null);
        }

        private static IEnumerable<T> LoadComponents<T>(XElement root, Action<T, XElement> additionalLoaders) where T : ModelComponentBase, new()
        {
            if (root == null)
            {
                yield break;
            }
            foreach (var el in root.Elements())
            {
                var component = LoadComponent<T>(el);
                if (additionalLoaders != null)
                {
                    additionalLoaders(component, el);
                }
                yield return component;
            }
        }

        private static T LoadComponent<T>(XElement e, bool typeFromAttribute = false) where T : ModelComponentBase, new()
        {
            return LoadComponent(e, typeFromAttribute, new T());
        }

        private static T LoadComponent<T>(XElement e, bool typeFromAttribute, T instance) where T : ModelComponentBase
        {
            var named = instance as NamedModelComponentBase;
            if (named != null)
            {
                named.Name = e.AttributeValueOrDefault("name");
            }

            if (typeFromAttribute)
            {
                instance.Type = e.AttributeValueOrDefault("type");
            }
            else
            {
                instance.Type = e.Name.LocalName;
            }

            instance.Value = e.Value;
            instance.Version = e.AttributeValueOrDefault<Version>(
                "version", Version.Parse, new Version(1, 0));

            foreach (var attr in e.Attributes())
            {
                instance.Attributes[attr.Name.LocalName] = attr.Value;
            }

            return instance;
        }
    }
}
