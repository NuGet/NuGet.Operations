using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Secrets
{
    public abstract class SecretStoreProvider
    {
        public abstract Task<SecretStore> Create(string store, IEnumerable<string> allowedUsers);
        public abstract Task<SecretStore> Open(string store);
        public abstract IEnumerable<string> ListStores();

        public static string GetEnvironmentStoreName(string environment)
        {
            return "env#" + environment;
        }
    }
}
