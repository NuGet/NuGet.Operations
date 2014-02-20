using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NuGet.Services.Operations.Secrets
{
    public class Secret
    {
        private List<SecretAuditEntry> _auditLog;

        public string Key { get; private set; }
        public string Value { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public DateTime? ExpiryUtc { get; private set; }

        public IReadOnlyList<SecretAuditEntry> AuditLog { get; private set; }

        [JsonConstructor]
        internal Secret(string key, string value, DateTime createdUtc, DateTime? expiryUtc, IEnumerable<SecretAuditEntry> auditLog)
        {
            Key = key;
            Value = value;
            CreatedUtc = createdUtc;
            ExpiryUtc = expiryUtc;

            _auditLog = auditLog.ToList();
            AuditLog = _auditLog.AsReadOnly();
        }
        
        public Secret(string key, string value, DateTime createdUtc, DateTime? expiryUtc) : this(key, value, createdUtc, expiryUtc, Enumerable.Empty<SecretAuditEntry>())
        {
        }

        public void AddAuditEntry(SecretAuditEntry entry)
        {
            _auditLog.Add(entry);
        }

        internal void Update(Secret secret)
        {
            Value = secret.Value;
            ExpiryUtc = secret.ExpiryUtc;
        }
    }
}
