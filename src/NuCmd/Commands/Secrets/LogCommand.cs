using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NuGet.Services.Operations;
using PowerArgs;

namespace NuCmd.Commands.Secrets
{
    [Description("Retrieves the audit log for a secret from the secret store")]
    public class LogCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The name of the key to get")]
        public string Key { get; set; }

        protected override async Task OnExecute()
        {
            // Open the store
            var store = await OpenSecretStore();

            // Read the secret
            var log = await store.ReadAuditLog(Key);

            // Write the log
            await Console.WriteInfoLine(Strings.Secrets_LogCommand_AuditLog, Key);
            await Console.WriteTable(log.OrderByDescending(e => e.TimestampUtc));
        }
    }
}
