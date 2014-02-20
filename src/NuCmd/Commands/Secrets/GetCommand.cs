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
    [Description("Retrieves a value from the secret store")]
    public class GetCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The name of the key to get")]
        public string Key { get; set; }

        [ArgShortcut("clip")]
        [ArgDescription("Set this switch to copy the value into the clipboard instead of printing it to the console")]
        public bool CopyToClipboard { get; set; }

        protected override async Task OnExecute()
        {
            // Open the store
            var store = await OpenSecretStore();

            // Read the secret
            var secret = await store.Read(Key, "nucmd get");

            // Write the value
            if (secret == null)
            {
                await Console.WriteInfoLine(Strings.Secrets_GetCommand_SecretDoesNotExist, Key);
            }
            else if (CopyToClipboard)
            {
                await STAHelper.InSTAThread(() => Clipboard.SetText(secret.Value));
                await Console.WriteInfoLine(Strings.Secrets_GetCommand_SecretCopied, Key);
            }
            else
            {
                await Console.WriteInfoLine(Strings.Secrets_GetCommand_SecretValue, Key, secret.Value);
            }
        }
    }
}
