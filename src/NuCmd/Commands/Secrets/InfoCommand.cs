// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Secrets
{
    [Description("Reads the metadata for the datacenter's secret store.")]
    public class InfoCommand : SecretStoreCommandBase
    {
        protected override async Task OnExecute()
        {
            var store = await OpenSecretStore();
            await Console.WriteInfoLine(Strings.Secret_CreateStoreCommand_StoreHeader, store.Metadata.Datacenter);
            foreach (var user in store.Metadata.AllowedUsers)
            {
                await Console.WriteInfoLine("* " + user);
            }
        }
    }
}
