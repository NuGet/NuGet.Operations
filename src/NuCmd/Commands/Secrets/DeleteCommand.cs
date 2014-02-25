﻿using System;
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
    [Description("Deletes a value from the secret store")]
    public class DeleteCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The name of the key to delete")]
        public string Key { get; set; }

        protected override async Task OnExecute()
        {
            // Open the store
            var store = await OpenSecretStore();

            // Delete the secret
            if (await store.Delete(Key, Datacenter, "nucmd delete"))
            {
                await Console.WriteInfoLine(Strings.Secrets_DeleteCommand_Deleted, Key);
            }
            else
            {
                await Console.WriteInfoLine(Strings.Secrets_DeleteCommand_NoSecret, Key);
            }
        }
    }
}
