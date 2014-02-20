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
    [Description("Retrieves a list of available secrets in the store")]
    public class ListCommand : SecretStoreCommandBase
    {
        protected override async Task OnExecute()
        {
            // Open the store
            var store = await OpenSecretStore();

            // Read the secret
            await Console.WriteInfoLine(Strings.Secrets_ListCommand_Secrets);
            foreach (var name in store.List())
            {
                await Console.WriteInfoLine("* " + name);
            }
        }
    }
}
