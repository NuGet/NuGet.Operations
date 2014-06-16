using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using NuGet.Services;
using NuGet.Services.Operations.Model;
using PowerArgs;
using Dapper;
using System.Windows.Forms;
using NuCmd.Models;
using System.ComponentModel;
using NuGet.Services.Operations.Secrets;
using System.Text.RegularExpressions;

namespace NuCmd.Commands.Db
{
    [Description("Deletes the specified SQL user from the database server.")]
    public class DeleteUserCommand : DatabaseCommandBase
    {
        private static readonly Regex BaseNameExtractor = new Regex(@"^(?<base>.*)_\d\d\d\d[A-Z][a-z][a-z]\d\d");

        [ArgRequired]
        [ArgShortcut("u")]
        [ArgPosition(0)]
        [ArgDescription("The user to delete")]
        public string User { get; set; }

        protected override async Task OnExecute()
        {
            var connInfo = await GetSqlConnectionInfo();

            // Connect to master to get the login
            SqlLogin login;
            IList<string> databases;
            using (var connection = await connInfo.Connect("master"))
            {
                login = (await connection.QueryAsync<SqlLogin>(@"
                    SELECT name, sid, create_date, modify_date 
                    FROM sys.sql_logins
                    WHERE name = @name", new { name = User })).FirstOrDefault();
                databases = (await connection.QueryAsync<string>("SELECT name FROM sys.databases WHERE name NOT LIKE 'copytemp_%'")).ToList();

                // Drop the login
                if (login != null)
                {
                    await Console.WriteInfoLine(String.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Db_DeleteUserCommand_DroppingLogin,
                            login.name,
                            "master"));
                    if (!WhatIf)
                    {
                        await connection.QueryAsync<int>("DROP LOGIN [" + login.name + "]");
                    }

                    // Drop the user if present
                    var user = (await connection.QueryAsync<SqlUser>(@"
                        SELECT uid, name, sid
                        FROM sys.sysusers
                        WHERE sid = @sid", new { sid = login.sid })).FirstOrDefault();
                    if (user != null)
                    {
                        await Console.WriteInfoLine(String.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Db_DeleteUserCommand_DroppingUser,
                            user.name,
                            "master"));

                        if (!WhatIf)
                        {
                            await connection.QueryAsync<int>("DROP USER [" + user.name + "]");
                        }
                    }
                    else
                    {
                        await Console.WriteInfoLine(String.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Db_DeleteUserCommand_NoUser,
                            User,
                            "master"));
                    }
                }
                else
                {
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_DeleteUserCommand_NoLogin,
                        User,
                        "master"));
                }
            }

            // Connect to the databases to drop the user
            foreach (var database in databases)
            {
                using (var connection = await connInfo.Connect(database))
                {
                    var user = (await connection.QueryAsync<SqlUser>(@"
                        SELECT uid, name, sid
                        FROM sys.sysusers
                        WHERE name = @name", new { name = User })).FirstOrDefault();

                    if (user != null)
                    {
                        // Drop the user
                        await Console.WriteInfoLine(String.Format(
                                CultureInfo.CurrentCulture,
                                Strings.Db_DeleteUserCommand_DroppingUser,
                                user.name,
                                database));
                        if (!WhatIf)
                        {
                            await connection.QueryAsync<int>("DROP USER [" + user.name + "]");
                        }
                    }
                    else
                    {
                        await Console.WriteInfoLine(String.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Db_DeleteUserCommand_NoUser,
                            User,
                            database));
                    }
                }
            }

            // Clean up the secret store
            await CleanSecrets(connInfo);
        }

        private async Task CleanSecrets(SqlConnectionInfo connInfo)
        {
            if (Session.CurrentEnvironment == null)
            {
                return;
            }

            var secrets = await GetEnvironmentSecretStore(Session.CurrentEnvironment);
            if (secrets == null)
            {
                return;
            }

            var loginSecretName = new SecretName("sqldb." + connInfo.GetServerName() + ":logins." + User, datacenter: null);
            var secret = await secrets.Read(loginSecretName, "nucmd db deleteuser");
            if (secret != null)
            {
                await Console.WriteInfoLine(Strings.Db_DeleteUserCommand_DeletingSecret, loginSecretName.Name);
                await secrets.Delete(loginSecretName, "nucmd db deleteuser");
            }

            // Check if there is a link that points at this user
            var match = BaseNameExtractor.Match(User);
            if (!match.Success)
            {
                return;
            }

            var userSecretName = new SecretName("sqldb." + connInfo.GetServerName() + "users." + match.Groups["base"].Value);
            secret = await secrets.Read(userSecretName, "nucmd db deleteuser");
            if (String.Equals(secret.Value, loginSecretName.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                await Console.WriteInfoLine(Strings.Db_DeleteUserCommand_DeletingSecret, userSecretName.Name);
                await secrets.Delete(userSecretName, "nucmd db deleteuser");
            }
        }
    }
}
