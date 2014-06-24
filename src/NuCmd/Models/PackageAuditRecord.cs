using System;
using System.Data;

namespace NuCmd.Models
{
    public class PackageAuditRecord : AuditRecord<PackageAuditAction>
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Hash { get; set; }

        public DataTable PackageRecord { get; set; }
        public DataTable RegistrationRecord { get; set; }

        public string Reason { get; set; }

        public PackageAuditRecord(string id, string version, string hash, DataTable packageRecord, DataTable registrationRecord, PackageAuditAction action, string reason)
            : base(action)
        {
            Id = id;
            Version = version;
            Hash = hash;
            PackageRecord = packageRecord;
            RegistrationRecord = registrationRecord;
            Reason = reason;
        }

        public override string GetPath()
        {
            return String.Format(
                "{0}/{1}",
                Id.ToLowerInvariant(),
                SemanticVersionHelper.Normalize(Version).ToLowerInvariant());
        }
    }

    public enum PackageAuditAction
    {
        Deleted
    }
}
