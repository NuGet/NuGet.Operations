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
    [Description("Displays a list of schemas on the database, and the users with access to them.")]
    public class SchemasCommand : DatabaseCommandBase
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
            IList<SqlPermission> perms;
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
                        s.name AS object_name
                    FROM sys.schemas s
                    LEFT OUTER JOIN sys.database_permissions p ON p.class_desc = 'SCHEMA' AND p.major_id = s.schema_id
                    LEFT OUTER JOIN sys.database_principals u ON u.principal_id = p.grantee_principal_id
                    WHERE u.[type] = 'S'
                ")).ToList();
            }

            // Group by name and write
            await Console.WriteTable(perms.Join(logins, p => p.sid_string, l => l.sid_string, (p, l) => new
            {
                Schema = p.object_name,
                Login = l.name,
                User = p.name,
                Status = p.state_desc,
                Type = p.permission_name
            })
            .Where(a => a.Status == "GRANT" && a.Type == "CONTROL")
            .GroupBy(a => a.Schema)
            .Select(g => new {
                Schema = g.Key,
                Logins = String.Join(",", g.Select(a => a.Login))
            }));
        }
    }
}
