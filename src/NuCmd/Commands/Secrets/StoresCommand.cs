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
    [Description("Lists the stores available.")]
    public class StoresCommand : SecretStoreProviderCommandBase
    {
        protected override async Task OnExecute()
        {
            var provider = CreateProvider();
            var stores = provider.ListStores();
            await Console.WriteInfoLine(Strings.Secret_StoresCommand_Stores, StoreRoot);
            foreach (var store in stores)
            {
                await Console.WriteInfoLine("* " + store);
            }
        }
    }
}
