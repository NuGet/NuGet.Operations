using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Secrets
{
    public abstract class SecretStore
    {
        public SecretStoreMetadata Metadata { get; private set; }

        protected SecretStore(SecretStoreMetadata metadata)
        {
            Metadata = metadata;
        }

        public abstract Task Write(Secret secret);
        public abstract Task<Secret> Read(string key);
        public abstract IEnumerable<string> List();
        public abstract Task<IEnumerable<SecretAuditEntry>> ReadAuditLog(string key);
    }
}
