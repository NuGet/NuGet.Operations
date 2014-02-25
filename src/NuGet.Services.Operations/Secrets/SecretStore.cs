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

        public virtual Task<bool> Delete(string name, int? datacenter, string clientOperation)
        {
            return Delete(new SecretName(name, datacenter), clientOperation);
        }

        public abstract Task<bool> Delete(SecretName name, string clientOperation);

        public virtual Task<bool> Undelete(string name, int? datacenter, string clientOperation)
        {
            return Undelete(new SecretName(name, datacenter), clientOperation);
        }

        public abstract Task<bool> Undelete(SecretName name, string clientOperation);

        public abstract Task Write(Secret secret, string clientOperation);

        public virtual Task<Secret> Read(string name, int? datacenter, string clientOperation)
        {
            return Read(new SecretName(name, datacenter), clientOperation);
        }

        public abstract Task<Secret> Read(SecretName key, string clientOperation);

        public virtual IEnumerable<SecretListItem> List()
        {
            return List(includeDeleted: false);
        }

        public abstract IEnumerable<SecretListItem> List(bool includeDeleted);

        public virtual Task<IEnumerable<SecretAuditEntry>> ReadAuditLog(string name, int? datacenter)
        {
            return ReadAuditLog(new SecretName(name, datacenter));
        }

        public abstract Task<IEnumerable<SecretAuditEntry>> ReadAuditLog(SecretName key);
    }
}
