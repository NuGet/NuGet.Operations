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
    [Description("Retrieves the audit log for a secret from the secret store")]
    public class LogCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The name of the key to get")]
        public string Key { get; set; }

        [ArgPosition(1)]
        [ArgShortcut("l")]
        [ArgDescription("The number of entries to get")]
        [PowerArgs.DefaultValue(10)]
        public int Limit { get; set; }

        protected override async Task OnExecute()
        {
            // Open the store
            var store = await OpenSecretStore();

            // Read the secret
            var log = (await store.ReadAuditLog(Key, Datacenter)).ToList();

            // Write the log
            await Console.WriteInfoLine(Strings.Secrets_LogCommand_AuditLog, Key);
            var entries = log.OrderByDescending(e => e.TimestampUtc).Take(Limit).ToList();
            await Console.WriteTable(entries);
            await Console.WriteInfoLine(Strings.Secrets_LogCommand_WroteEntries, entries.Count, log.Count);
        }
    }
}
