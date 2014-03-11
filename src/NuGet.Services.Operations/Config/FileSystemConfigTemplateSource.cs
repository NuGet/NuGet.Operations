using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Services.Operations.Model;

namespace NuGet.Services.Operations.Config
{
    public class FileSystemConfigTemplateSource : ConfigTemplateSource
    {
        public static string RelativeAppModelType = "relativePath";
        public static string AbsoluteAppModelType = "path";

        public string RootDirectory { get; private set; }

        public FileSystemConfigTemplateSource(string rootDirectory)
        {
            RootDirectory = rootDirectory;
        }

        public override string ReadConfigTemplate(Service service)
        {
            // Generate file name
            string fileName = Path.Combine(
                RootDirectory,
                service.FullName + ".cscfg.template");
            if (File.Exists(fileName))
            {
                return File.ReadAllText(fileName);
            }
            else
            {
                return null;
            }
        }
    }
}
