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
    [Description("Deploys the specified DACPAC file to the database.")]
    public class DeployCommand : DacCommandBase
    {
        [ArgRequired]
        [ArgShortcut("p")]
        [ArgExistingFile]
        [ArgDescription("The DACPAC file built by the build server.")]
        public string DacPac { get; set; }

        protected override async Task OnExecute()
        {
            var connInfo = await GetSqlConnectionInfo();
            var package = DacPackage.Load(DacPac);
            var services = ConnectDac(connInfo);

            await Console.WriteInfoLine(String.Format(
                CultureInfo.CurrentCulture,
                Strings.Db_DeployCommand_Deploying,
                package.Name,
                connInfo.ConnectionString.InitialCatalog,
                connInfo.ConnectionString.DataSource));
            if (!WhatIf)
            {
                services.Deploy(
                    package,
                    connInfo.ConnectionString.InitialCatalog,
                    upgradeExisting: true,
                    options: new DacDeployOptions()
                    {
                        BlockOnPossibleDataLoss = true
                    });
            }
            await Console.WriteInfoLine(Strings.Db_DeployCommand_Deployed);
        }
    }
}
