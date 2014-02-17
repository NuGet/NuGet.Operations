using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Model
{
    public abstract class SecretStoreConnection
    {
        public abstract Task<Secret> Get(string scope, string resourceType, string resourceName, string key);
        public abstract Task Set(Secret value);

        public static SecretStoreConnection Open(SecretStore store)
        {
            if (String.Equals(store.Type, SharepointSecretStoreConnection.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                return new SharepointSecretStoreConnection(store);
            }
            throw new InvalidOperationException(String.Format(
                CultureInfo.CurrentCulture,
                Strings.SecretStoreConnection_UnknownSecretStoreType,
                store.Type));
        }
    }
}
