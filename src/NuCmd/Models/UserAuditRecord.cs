using System;
using System.Data;

namespace NuCmd.Models
{
    public class UserAuditRecord : AuditRecord<UserAuditAction>
    {
        public string Username { get; set; }
        public string EmailAddress { get; set; }
        public DataTable UserRecord { get; set; }

        public string Reason { get; set; }

        public UserAuditRecord(string username, string emailAddress, DataTable userRecord, UserAuditAction action, string reason)
            : base(action)
        {
            Username = username;
            EmailAddress = emailAddress;
            UserRecord = userRecord;
            Reason = reason;
        }

        public override string GetPath()
        {
            return Username.ToLowerInvariant();
        }
    }

    public enum UserAuditAction
    {
        Deleted
    }
}
