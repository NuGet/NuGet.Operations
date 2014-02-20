using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuCmd.Models;
using Dapper;
using Microsoft.SqlServer.Dac;

namespace NuCmd.Commands.Db
{
    public class DacsCommand : DatabaseCommandBase
    {
        protected override async Task OnExecute()
        {
            var connInfo = await GetSqlConnectionInfo();
            
            // Connect to master to get a list of logins
            IList<SqlDac> dacs;
            using (var connection = await connInfo.Connect("master"))
            {
                dacs = (await connection.QueryAsync<SqlDac>(@"
                    SELECT [instance_id]
                          ,[instance_name]
                          ,[type_name]
                          ,[type_version]
                          ,[description]
                          ,[type_stream]
                          ,[date_created]
                          ,[created_by]
                    FROM [dbo].[sysdac_instances_internal]"))
                    .ToList();
            }

            await Console.WriteTable(dacs, d => new
            {
                Name = d.instance_name,
                Type = d.type_name,
                Version = d.type_version,
                CreatedOn = d.date_created,
                CreatedBy = d.created_by
            });
        }
    }
}
