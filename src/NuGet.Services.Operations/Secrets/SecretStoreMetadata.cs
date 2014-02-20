using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Secrets
{
    public class SecretStoreMetadata
    {
        public string Datacenter { get; set; }
        public Version Version { get; set; }
        public IEnumerable<string> AllowedUsers { get; set; }
    }
}
