using System;
using System.Collections.Generic;
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
        public string MachineName { get; private set; }
        public string MachineIP { get; private set; }
        public DateTime TimestampUtc { get; private set; }
        public SecretAuditAction Action { get; private set; }

        public SecretAuditEntry(string user, string machineName, string machineIP, DateTime timestampUtc, SecretAuditAction action)
        {
            TimestampUtc = timestampUtc;
            User = user;
            Action = action;
            MachineName = machineName;
            MachineIP = machineIP;
        }

        public static async Task<SecretAuditEntry> CreateForLocalUser(SecretAuditAction action)
        {
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
                Environment.MachineName,
                ipAddress,
                DateTime.UtcNow,
                action);
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
        Retrieved
    }
}
