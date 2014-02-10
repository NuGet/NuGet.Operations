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
            var connInfo = GetSqlConnectionInfo();

            // Connect to master to get a list of logins
            IList<SqlLogin> logins;
            using (var connection = await connInfo.Connect("master"))
            {
                logins = (await connection.QueryAsync<SqlLogin>("SELECT name, sid, create_date, modify_date FROM sys.sql_logins"))
                    .ToList();
            }

            // Connect to the database to get a list of permissions
            ILookup<string, SqlPermission> perms;
            using (var connection = await connInfo.Connect())
            {
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
                ")).ToList().ToLookup(p => p.sid_string);
            }

            await Console.WriteInfoLine(String.Format(
                CultureInfo.CurrentCulture,
                Strings.Db_UsersCommand_DisplayingPermissions,
                connInfo.ConnectionString.InitialCatalog));

            // Join in memory!
            var items = logins.SelectMany(l =>
            {
                var e = perms[l.sid_string];
                if (!e.Any())
                {
                    return new[] { Tuple.Create(l, new SqlPermission()) };
                }
                return e.Select(p => Tuple.Create(l, p));
            }).Select(t => new
            {
                Login = t.Item1.name,
                User = t.Item2.name,
                ObjectType = t.Item2.class_desc,
                ObjectName = String.Equals(t.Item2.class_desc, "DATABASE", StringComparison.OrdinalIgnoreCase) ?
                    connInfo.ConnectionString.InitialCatalog :
                    t.Item2.object_name,
                Permission = t.Item2.permission_name,
                Status = t.Item2.state_desc,
                LoginCreated = t.Item1.create_date,
                LoginModified = t.Item1.modify_date
            });

            // Display!
            await Console.WriteTable(items);
        }
    }
}
