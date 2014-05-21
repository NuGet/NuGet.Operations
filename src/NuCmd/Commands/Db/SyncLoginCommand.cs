using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using NuGet.Services;
using NuGet.Services.Operations;
using PowerArgs;

namespace NuCmd.Commands.Db
{
    [Description("Sychronizes a login from DC0 to a target datacenter")]
    public class SyncLoginCommand : DatacenterCommandBase
    {
        [ArgPosition(0)]
        [ArgShortcut("na")]
        [ArgDescription("The full name of the login")]
        public string Name { get; set; }

        [ArgRequired]
        [ArgShortcut("db")]
        [ArgDescription("The type of the SQL Database to create the user on")]
        public KnownSqlConnection Database { get; set; }

        [ArgShortcut("pass")]
        [ArgDescription("The password to give the user in the target datacenter. Defaults to the same password as was used in DC0")]
        public string Password { get; set; }

        protected override async Task OnExecute()
        {
            // Ensure we have a datacenter parameter
            GetDatacenter(required: true);

            var dc0conn = await GetSqlConnectionInfo(
                0,
                Database.ToString(),
                specifiedAdminUser: null,
                specifiedAdminPassword: null,
                promptForPassword: false);

            var targetConn = await GetSqlConnectionInfo(
                Datacenter.Value,
                Database.ToString(),
                specifiedAdminUser: null,
                specifiedAdminPassword: null,
                promptForPassword: false);

            byte[] sid;
            using (var connection = await dc0conn.Connect("master"))
            {
                await Console.WriteInfoLine(Strings.Db_SyncUserCommand_RetrievingSid, Name, dc0conn.GetServerName());
                sid = (await connection.QueryAsync<byte[]>(
                    "SELECT TOP 1 sid FROM sys.sql_logins WHERE name = @Name",
                    new { Name })).SingleOrDefault();
                if (sid == null)
                {
                    await Console.WriteErrorLine(Strings.Db_SyncUserCommand_NoLoginInDC0, Name, dc0conn.GetServerName());
                    return;
                }
            }

            // Get the DC0 password
            Password = Password ?? (await GetExistingPassword(dc0conn));
            if (String.IsNullOrEmpty(Password))
            {
                await Console.WriteErrorLine(Strings.Db_SyncUserCommand_NoPassword);
                return;
            }

            // Got the sid, now create a login in the other datacenter with the same sid
            using (var connection = await targetConn.Connect("master"))
            {
                var sidString = BitConverter.ToString(sid).Replace("-", "");

                await Console.WriteInfoLine(Strings.Db_SyncUserCommand_CreatingLogin, Name, targetConn.GetServerName());
                await connection.QueryAsync<int>(
                    "CREATE LOGIN [" + Name + "] WITH PASSWORD = '" + Password + "', SID=0x" + sidString);
            }
        }

        private async Task<string> GetExistingPassword(SqlConnectionInfo dc0conn)
        {
            var cstr = await GetSecretOrDefault("sqldb." + dc0conn.GetServerName() + ":logins." + Name);
            return String.IsNullOrEmpty(cstr) ? null : new SqlConnectionStringBuilder(cstr).Password;
        }
    }
}
