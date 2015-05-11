// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
    [Description("Undeletes a value from the secret store")]
    public class UndeleteCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The name of the key to undelete")]
        public string Key { get; set; }

        protected override async Task OnExecute()
        {
            // Open the store
            var store = await OpenSecretStore();

            // Delete the secret
            if (await store.Undelete(Key, Datacenter, "nucmd delete"))
            {
                await Console.WriteInfoLine(Strings.Secrets_UndeleteCommand_Restored, Key);
            }
            else
            {
                await Console.WriteInfoLine(Strings.Secrets_UndeleteCommand_NoSecret, Key);
            }
        }
    }
}
