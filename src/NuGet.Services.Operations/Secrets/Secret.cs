﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NuGet.Services.Operations.Secrets
{
    public class Secret
    {
        private List<SecretAuditEntry> _auditLog;

        public SecretName Name { get; private set; }
        public string Value { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public DateTime? ExpiryUtc { get; private set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public SecretType Type { get; private set; }

        public IReadOnlyList<SecretAuditEntry> AuditLog { get; private set; }

        [JsonConstructor]
        internal Secret(SecretName name, string value, DateTime createdUtc, DateTime? expiryUtc, SecretType type, IEnumerable<SecretAuditEntry> auditLog)
        {
            Name = name;
            Value = value;
            CreatedUtc = createdUtc;
            ExpiryUtc = expiryUtc;
            Type = type;

            _auditLog = auditLog.ToList();
            AuditLog = _auditLog.AsReadOnly();
        }

        public Secret(SecretName name, string value, DateTime createdUtc, DateTime? expiryUtc, SecretType type)
            : this(name, value, createdUtc, expiryUtc, type, Enumerable.Empty<SecretAuditEntry>())
        {
        }

        public void AddAuditEntry(SecretAuditEntry entry)
        {
            _auditLog.Add(entry);
        }

        internal void Update(Secret secret)
        {
            Value = secret.Value;
            Type = secret.Type;
            ExpiryUtc = secret.ExpiryUtc;
        }
    }

    public enum SecretType
    {
        Password
    }
}
