using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NuGet.Services.Operations.Helpers
{
    public static class PackageConfigParser
    {
        [Serializable()]
        [System.Xml.Serialization.XmlTypeAttribute()]
        public class Package
        {
            [System.Xml.Serialization.XmlAttributeAttribute("id")]
            public string Id { get; set; }

            [System.Xml.Serialization.XmlAttributeAttribute("version")]
            public string Version { get; set; }

            [System.Xml.Serialization.XmlAttributeAttribute("targetFramework")]
            public string TargetFramework { get; set; }
        }

        [Serializable()]
        [System.Xml.Serialization.XmlRoot("packages")]
        public class PackageCollection
        {
            [System.Xml.Serialization.XmlElement("package")]
            public Package[] Packages { get; set; }
        }

        public static List<Package> GetIdAndVersionFromPackagesConfig(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            List<Package> packagesInPackagesConfig;
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PackageCollection));
            StreamReader reader = new StreamReader(path);
            
            if (serializer == null)
            {
                return null;
            }

            var packages = (PackageCollection)serializer.Deserialize(reader);
            packagesInPackagesConfig = packages.Packages.ToList();
            reader.Close();
            return packagesInPackagesConfig;
        }

    }
}
