using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet;

namespace RenderBlobs
{
    class Gallery
    {
        public const string PackageDefaultIcon = "http://nuget.org/Content/Images/packageDefaultIcon-50x50.png";

        public Gallery()
        {
            PackageRegistrations = new Dictionary<string, PackageRegistration>();
            Owners = new Dictionary<string, Owner>();
            Packages = new Dictionary<string, Package>();
            PackageDependencies = new Dictionary<string, PackageDependency>();
        }

        public void MakeConnections()
        {
        }

        public IDictionary<string, PackageRegistration> PackageRegistrations
        {
            get;
            private set;
        }

        public IDictionary<string, Owner> Owners
        {
            get;
            private set;
        }

        public IDictionary<string, Package> Packages
        {
            get;
            private set;
        }

        public IDictionary<string, PackageDependency> PackageDependencies
        {
            get;
            private set;
        }

        public abstract class BaseDocumentObject
        {
            public abstract JObject CreateDocument();

            public virtual JObject CreateSummaryDocument()
            {
                return CreateDocument();
            }

            protected static string CreateUri(string name)
            {
                // this is where we would create the full, external URI here if we decided to do that
                return name;
            }
        }

        public class PackageRegistration : BaseDocumentObject
        {
            public PackageRegistration()
            {
                Packages = new SortedList<string, Package>();
                Owners = new Dictionary<string, Owner>();
            }

            public string Name
            {
                get { return "package/" + Id; }
            }

            public string Id { get; set; }
            public int DownloadCount { get; set; }

            public IDictionary<string, Owner> Owners
            {
                get;
                private set;
            }

            public IDictionary<string, Package> Packages
            {
                get;
                private set;
            }

            public Package Latest
            {
                get { return Packages[LatestVersion()]; }
            }

            private string LatestVersion()
            {
                string last = Packages.ToList().OrderBy((p) => new SemanticVersion(p.Key)).Last().Key;
                return last;
            }

            public override JObject CreateDocument()
            {
                if (Packages.Count == 0)
                {
                    return null;
                }

                return CreateDocument(LatestVersion());
            }

            public JObject CreateDocument(string packageVersion)
            {
                JObject root = new JObject();

                JObject details = Packages[packageVersion].GetDetails();
                root.Add("details", details);

                root.Add("downloads", DownloadCount);

                JArray owners = new JArray();
                foreach (Gallery.Owner owner in Owners.Values)
                {
                    owners.Add(owner.CreateSummaryDocument());
                }
                root.Add("owners", owners);

                JArray packages = new JArray();
                foreach (KeyValuePair<string, Gallery.Package> package in Packages.ToList().OrderBy((p) => new SemanticVersion(p.Key)).Reverse())
                {
                    packages.Add(package.Value.CreateSummaryDocument());
                }
                root.Add("versions", packages);

                return root;
            }

            public override JObject CreateSummaryDocument()
            {
                if (Packages.Count == 0)
                {
                    return null;
                }

                Package package = Packages[LatestVersion()];

                JObject root = new JObject();
                root.Add("uri", CreateUri(Name));
                root.Add("id", Id);
                root.Add("downloads", DownloadCount);
                root.Add("title", package.Title ?? Id);
                root.Add("description", package.Description);
                root.Add("iconUrl", (package.IconUrl ?? new Uri(PackageDefaultIcon)).AbsoluteUri);
                return root;
            }
        }

        public class Owner : BaseDocumentObject
        {
            public Owner()
            {
                PackageRegistrations = new Dictionary<string, PackageRegistration>();
            }

            public string Name
            {
                get { return "owner/" + UserName; }
            }

            public string UserName { get; set; }

            public IDictionary<string, PackageRegistration> PackageRegistrations
            {
                get;
                private set;
            }

            public override JObject CreateDocument()
            {
                JObject root = new JObject();

                root.Add("userName", UserName);

                int downloads = 0;

                JArray packageRegistrations = new JArray();
                foreach (Gallery.PackageRegistration packageRegistration in PackageRegistrations.Values)
                {
                    packageRegistrations.Add(packageRegistration.CreateSummaryDocument());

                    downloads += packageRegistration.DownloadCount;
                }
                root.Add("packages", packageRegistrations);

                root.Add("downloads", downloads);

                return root;
            }

            public override JObject CreateSummaryDocument()
            {
                JObject root = new JObject();
                root.Add("uri", CreateUri(Name));
                root.Add("userName", UserName);
                return root;
            }
        }

        public class Package : BaseDocumentObject
        {
            public Package()
            {
                DependsOn = new List<PackageDependency>();
                Authors = new List<string>();
            }

            public string Name
            {
                get { return "package/" + PackageRegistration.Id + "/" + Version; }
            }

            public PackageRegistration PackageRegistration { get; set; }

            public string Copyright { get; set; }
            public DateTime Created { get; set; }
            public string Description { get; set; }
            public int DownloadCount { get; set; }
            public Uri ExternalPackageUrl { get; set; }
            public string HashAlgorithm { get; set; }
            public string Hash { get; set; }
            public Uri IconUrl { get; set; }
            public bool IsLatest { get; set; }
            public DateTime LastUpdated { get; set; }
            public Uri LicenseUrl { get; set; }
            public DateTime Published { get; set; }
            public long PackageFileSize { get; set; }
            public Uri ProjectUrl { get; set; }
            public bool RequiresLicenseAcceptance { get; set; }
            public string Summary { get; set; }
            public string Tags { get; set; }
            public string Title { get; set; }
            public string Version { get; set; }
            public string FlattenedAuthors { get; set; }
            public string FlattenedDependencies { get; set; }
            public bool IsLatestStable { get; set; }
            public bool Listed { get; set; }
            public bool IsPrerelease { get; set; }
            public string ReleaseNotes { get; set; }
            public string Language { get; set; }

            public IList<PackageDependency> DependsOn
            {
                get;
                private set;
            }

            public IList<string> Authors
            {
                get;
                private set;
            }

            public JObject GetDetails()
            {
                JObject root = new JObject();

                root.Add("copyright", Copyright);
                //root.Add("created", Created);
                root.Add("description", Description);
                root.Add("downloads", DownloadCount);
                //root.Add("externalPackageUrl", ExternalPackageUrl);
                root.Add("hashAlgorithm", HashAlgorithm);
                root.Add("hash", Hash);
                root.Add("iconUrl", (IconUrl ?? new Uri(Gallery.PackageDefaultIcon)).AbsoluteUri);
                root.Add("isLatest", IsLatest);
                //root.Add("lastUpdated", LastUpdated);
                root.Add("lastUpdated", Published);
                root.Add("licenseUrl", LicenseUrl != null ? LicenseUrl.AbsoluteUri : null);
                //root.Add("published", Published);
                root.Add("packageFileSize", PackageFileSize);
                root.Add("projectUrl", ProjectUrl != null ? ProjectUrl.AbsoluteUri : null);
                root.Add("requiresLicenseAcceptance", RequiresLicenseAcceptance);
                root.Add("summary", Summary);
                root.Add("title", Title ?? PackageRegistration.Id);
                root.Add("version", Version);
                //root.Add("flattenedAuthors", FlattenedAuthors);
                root.Add("isLatestStable", IsLatestStable);
                root.Add("listed", Listed);
                root.Add("isPrerelease", IsPrerelease);
                root.Add("releaseNotes", ReleaseNotes);
                root.Add("language", Language);

                root.Add("id", PackageRegistration.Id);

                JArray tags = new JArray();
                foreach (string tag in ExtractTags(Tags))
                {
                    tags.Add(tag);
                }
                root.Add("tags", tags);

                JArray packageDependencies = new JArray();
                foreach (Gallery.PackageDependency packageDependency in DependsOn)
                {
                    packageDependencies.Add(packageDependency.CreateSummaryDocument());
                }
                root.Add("dependsOn", packageDependencies);

                JArray authors = new JArray();
                foreach (string author in Authors)
                {
                    authors.Add(author);
                }
                root.Add("authors", authors);

                return root;
            }

            public override JObject CreateDocument()
            {
                return PackageRegistration.CreateDocument(Version);
            }

            public override JObject CreateSummaryDocument()
            {
                JObject root = new JObject();
                root.Add("uri", CreateUri(Name));
                root.Add("version", Version);
                root.Add("downloads", DownloadCount);
                root.Add("lastUpdated", Published);
                root.Add("isLatest", IsLatest);
                root.Add("isLatestStable", IsLatestStable);
                return root;
            }

            public static IEnumerable<string> ExtractTags(string tags)
            {
                List<string> result = new List<string>();

                if (tags == null)
                {
                    return result;
                }

                string[] t = tags.Split(' ', ',');
                for (int i = 0; i < t.Length; i++)
                {
                    t[i] = t[i].TrimEnd(' ', ',');

                    if (t[i] == string.Empty || t[i] == ",")
                    {
                        continue;
                    }

                    result.Add(t[i]);
                }

                return result;
            }
        }

        public class PackageDependency : BaseDocumentObject
        {
            public PackageRegistration Dependency { get; set; }
            public string VersionSpec { get; set; }
            public string TargetFramework { get; set; }

            public string DisplayVersionSpec
            {
                get
                {
                    return (VersionSpec == null || VersionSpec == string.Empty) ?
                        string.Empty : VersionUtility.PrettyPrint(VersionUtility.ParseVersionSpec(VersionSpec));
                }
            }

            public override JObject CreateDocument()
            {
                JObject root = new JObject();
                root.Add("id", Dependency.Id);
                root.Add("uri", CreateUri(Dependency.Name));
                root.Add("versionSpec", VersionSpec);
                root.Add("displayVersionSpec", DisplayVersionSpec);
                root.Add("targetFramework", TargetFramework);
                return root;
            }
        }
    }
}
