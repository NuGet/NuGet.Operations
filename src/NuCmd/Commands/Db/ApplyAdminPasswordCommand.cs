// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Sql.Models;
using NuGet.Services;
using NuGet.Services.Operations;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Db
{
    [Description("Applies the admin password stored in the secret store to the database server")]
    public class ApplyAdminPasswordCommand : AzureConnectionCommandBase
    {
        [ArgRequired]
        [ArgShortcut("db")]
        [ArgDescription("The type of the SQL Database to create the user on")]
        public KnownSqlConnection Database { get; set; }

        [ArgShortcut("dc")]
        [ArgDescription("The datacenter to work in")]
        public int? Datacenter { get; set; }

        protected override async Task OnExecute(SubscriptionCloudCredentials credentials)
        {
            // Get the datacenter
            var dc = GetDatacenter(Datacenter ?? 0, required: true);

            // Find the server
            var server = dc.FindResource(ResourceTypes.SqlDb, Database.ToString());
            if (server == null)
            {
                await Console.WriteErrorLine(
                    Strings.Db_DatabaseCommandBase_NoDatabaseInDatacenter,
                    Datacenter.Value,
                    ResourceTypes.SqlDb,
                    Database.ToString());
                return;
            }
            var connStr = new SqlConnectionStringBuilder(server.Value);
            string serverName = Utils.GetServerName(connStr.DataSource);
            
            // Get the secret value
            var secrets = await GetEnvironmentSecretStore(Session.CurrentEnvironment);
            string secretName = "sqldb." + serverName + ":admin";
            
            // Connect to Azure
            using (var sql = CloudContext.Clients.CreateSqlManagementClient(credentials))
            {
                await Console.WriteInfoLine(Strings.Db_ApplyAdminPasswordCommand_ApplyingPassword, secretName, serverName);
                if (!WhatIf)
                {
                    var secret = await secrets.Read(new SecretName(secretName), "nucmd db applyadminpassword");
                    if (secret == null)
                    {
                        await Console.WriteErrorLine(Strings.Db_ApplyAdminPasswordCommand_NoPasswordInStore, serverName);
                        return;
                    }

                    await sql.Servers.ChangeAdministratorPasswordAsync(
                        serverName, new ServerChangeAdministratorPasswordParameters()
                        {
                            NewPassword = secret.Value
                        },
                        CancellationToken.None);
                }
                await Console.WriteInfoLine(Strings.Db_ApplyAdminPasswordCommand_AppliedPassword);
            }
        }
    }
}
