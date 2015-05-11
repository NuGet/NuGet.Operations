// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using PowerArgs;

namespace NuCmd.Commands.Db
{
    [Description("Deletes the NuGet DAC from the database.")]
    public class DeleteDacCommand : DacCommandBase
    {
        [ArgShortcut("dac")]
        [ArgDescription("The name of the DAC to remove. Usually the default value is what you want")]
        public string DacName { get; set; }

        protected override async Task OnExecute()
        {
            var connInfo = await GetSqlConnectionInfo();
            var services = ConnectDac(connInfo);

            if (!WhatIf)
            {
                await Console.WriteInfoLine(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DeleteDacCommand_Deleting,
                    connInfo.ConnectionString.InitialCatalog,
                    connInfo.ConnectionString.DataSource));
            }
            services.Unregister(connInfo.ConnectionString.InitialCatalog);
        }
    }
}
