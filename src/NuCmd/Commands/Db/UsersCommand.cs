// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
    [Description("Displays a list of users on the database server, and their level of access.")]
    public class UsersCommand : DatabaseCommandBase
    {
        protected override async Task OnExecute()
        {
            var connInfo = await GetSqlConnectionInfo();

            // Connect to master to get a list of logins
            IList<SqlLogin> logins;
            IList<string> databaseNames;
            using (var connection = await connInfo.Connect("master"))
            {
                await Console.WriteInfoLine(Strings.Db_UsersCommand_GatheringLoginAndDb);
                logins = (await connection.QueryAsync<SqlLogin>("SELECT name, sid, create_date, modify_date FROM sys.sql_logins"))
                    .ToList();
                databaseNames = (await connection.QueryAsync<string>("SELECT name FROM sys.databases WHERE name NOT LIKE 'copytemp_%'")).ToList();
            }

            // Get info for each database
            List<SqlUserGranting> grantings = new List<SqlUserGranting>();
            foreach(var dbName in databaseNames)
            {
                await Console.WriteInfoLine(Strings.Db_UsersCommand_GatheringUsers, dbName);
                grantings.AddRange(await FetchDatabaseUserInfo(connInfo, dbName));
            }

            // Order by user name and display
            await Console.WriteTable(grantings.OrderBy(g => g.Name).ThenBy(g => g.Database), g => new
            {
                g.Name,
                g.Database,
                g.Type,
                g.Target,
                g.Permission
            });
        }

        private async Task<IEnumerable<SqlUserGranting>> FetchDatabaseUserInfo(SqlConnectionInfo connInfo, string database)
        {
            IEnumerable<SqlPermission> perms;
            IEnumerable<SqlRoleMembership> roles;
            using(var connection = await connInfo.Connect(database))
            {
                // Fetch Permissions and role memberships
                perms = (await connection.QueryAsync<SqlPermission>(@"
                    SELECT
	                    u.principal_id,
	                    u.name,
	                    p.class_desc,
                        p.state_desc,
                        p.permission_name,
	                    u.[sid],
	                    (CASE WHEN p.class_desc = 'SCHEMA' THEN s.name 
		                      WHEN p.class_desc = 'DATABASE' THEN NULL END) AS object_name
                    FROM sys.database_permissions p
                    INNER JOIN sys.database_principals u ON p.grantee_principal_id = u.principal_id
                    LEFT OUTER JOIN sys.schemas s ON p.class_desc = 'SCHEMA' AND p.major_id = s.schema_id
                    WHERE u.[type] = 'S'
                ")).ToList();

                roles = (await connection.QueryAsync<SqlRoleMembership>(@"
                    SELECT mem_prin.name AS member, role_prin.name AS role, mem_prin.[sid] as [sid]
                    FROM sys.database_role_members mem
                    INNER JOIN sys.database_principals mem_prin ON mem.member_principal_id = mem_prin.principal_id
                    INNER JOIN sys.database_principals role_prin ON mem.role_principal_id = role_prin.principal_id
                ")).ToList();
            }

            return Enumerable.Concat(
                perms.Select(p => SqlUserGranting.Create(p, database)),
                roles.Select(r => SqlUserGranting.Create(r, database)));
        }

        private class SqlUserGranting
        {
            public string Database { get; set; }
            public string Name { get; set; }
            public string Sid { get; set; }
            public GrantingType Type { get; set; }
            public string Permission { get; set; }
            public string Target { get; set; }

            internal static SqlUserGranting Create(SqlPermission arg, string dbName)
            {
                string target =
                    arg.class_desc + (
                        String.Equals(arg.class_desc, "DATABASE", StringComparison.OrdinalIgnoreCase) ?
                            "" :
                            (":" + arg.object_name)
                    );


                return new SqlUserGranting()
                {
                    Database = dbName,
                    Name = arg.name,
                    Sid = arg.sid_string,
                    Type = GrantingType.Grant,
                    Target = target,
                    Permission = arg.permission_name
                };
            }

            internal static SqlUserGranting Create(SqlRoleMembership arg, string dbName)
            {
                return new SqlUserGranting()
                {
                    Database = dbName,
                    Name = arg.member,
                    Sid = arg.sid_string,
                    Type = GrantingType.Member,
                    Target = arg.role,
                    Permission = String.Empty
                };
            }
        }

        private enum GrantingType
        {
            Grant,
            Member
        }
    }
}
