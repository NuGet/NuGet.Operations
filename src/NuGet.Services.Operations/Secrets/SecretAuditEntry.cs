using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Secrets
{
    public class SecretAuditEntry
    {
        public string User { get; private set; }
        public string ClientOperation { get; private set; }
        public string ProcessName { get; private set; }
        public string MachineName { get; private set; }
        public string MachineIP { get; private set; }
        public DateTime TimestampUtc { get; private set; }
        public SecretAuditAction Action { get; private set; }
        public string OldValue { get; private set; }

        public SecretAuditEntry(string user, string clientOperation, string processName, string machineName, string machineIP, DateTime timestampUtc, SecretAuditAction action, string oldValue)
        {
            TimestampUtc = timestampUtc;
            ClientOperation = clientOperation;
            ProcessName = processName;
            User = user;
            Action = action;
            MachineName = machineName;
            MachineIP = machineIP;
            OldValue = oldValue;
        }

        public static Task<SecretAuditEntry> CreateForLocalUser(string clientOperation, SecretAuditAction action)
        {
            return CreateForLocalUser(clientOperation, action, oldValue: null);
        }

        public static async Task<SecretAuditEntry> CreateForLocalUser(string clientOperation, SecretAuditAction action, string oldValue)
        {
            Debug.Assert(action != SecretAuditAction.Changed || !String.IsNullOrEmpty(oldValue), Strings.SecretAuditEntry_OldValueRequired);
            if (action == SecretAuditAction.Changed && String.IsNullOrEmpty(oldValue))
            {
                throw new ArgumentException(Strings.SecretAuditEntry_OldValueRequired, "oldValue");
            }

            // Get the current IP address
            string ipAddress = null;
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                var entry = await Dns.GetHostEntryAsync(Dns.GetHostName());
                if (entry != null)
                {
                    ipAddress =
                        TryGetAddress(entry.AddressList, AddressFamily.InterNetworkV6) ??
                        TryGetAddress(entry.AddressList, AddressFamily.InterNetwork);
                }
            }
            
            // Return the entry
            return new SecretAuditEntry(
                Environment.UserDomainName + "\\" + Environment.UserName,
                clientOperation,
                Process.GetCurrentProcess().ProcessName,
                Environment.MachineName,
                ipAddress,
                DateTime.UtcNow,
                action,
                oldValue);
        }

        private static string TryGetAddress(IEnumerable<IPAddress> addrs, AddressFamily family)
        {
            return addrs.Where(a => a.AddressFamily == family).Select(a => a.ToString()).FirstOrDefault();
        }
    }

    public enum SecretAuditAction
    {
        Created,
        Changed,
        Retrieved,
        Deleted,
        Restored
    }
}
