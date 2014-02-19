using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace NuCmd.Models
{
    public class PackageRegistrationAuditRecord : AuditRecord<PackageRegistrationAuditAction>
    {
        public string Id { get; set; }
        
        public DataTable RegistrationRecord { get; set; }

        public string Reason { get; set; }

        public PackageRegistrationAuditRecord(string id, DataTable registrationRecord, PackageRegistrationAuditAction action, string reason)
            : base(action)
        {
            Id = id;
            RegistrationRecord = registrationRecord;
            Reason = reason;
        }

        public override string GetPath()
        {
            return String.Format(
                "{0}",
                Id.ToLowerInvariant());
        }
    }

    public enum PackageRegistrationAuditAction
    {
        Deleted
    }
}
