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

namespace NuCmd.Commands.Db
{
    [Description("Deletes the specified SQL user from the database server.")]
    public class DeleteUserCommand : DatabaseCommandBase
    {
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
            using (var connection = await connInfo.Connect("master"))
            {
                login = (await connection.QueryAsync<SqlLogin>(@"
                    SELECT name, sid, create_date, modify_date 
                    FROM sys.sql_logins
                    WHERE name = @name", new { name = User })).FirstOrDefault();

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

            // Connect to the database to drop the user
            if (login == null)
            {
                await Console.WriteWarningLine(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DeleteUserCommand_UnableToCheckUser,
                    User,
                    connInfo.ConnectionString.InitialCatalog));
            }
            else
            {
                using (var connection = await connInfo.Connect())
                {
                    var user = (await connection.QueryAsync<SqlUser>(@"
                    SELECT uid, name, sid
                    FROM sys.sysusers
                    WHERE sid = @sid", new { sid = login.sid })).FirstOrDefault();

                    if (user != null)
                    {
                        // Drop the user
                        await Console.WriteInfoLine(String.Format(
                                CultureInfo.CurrentCulture,
                                Strings.Db_DeleteUserCommand_DroppingUser,
                                user.name,
                                connInfo.ConnectionString.InitialCatalog));
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
                            connInfo.ConnectionString.InitialCatalog));
                    }
                }
            }
        }
    }
}
