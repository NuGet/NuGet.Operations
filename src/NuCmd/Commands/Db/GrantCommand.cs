using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using PowerArgs;

namespace NuCmd.Commands.Db
{
    [Description("Grants access to all databases to the provided login")]
    public class GrantCommand : DatabaseCommandBase
    {
        // Sql Azure requires that CREATE USER be the only statement in the batch :(
        private const string GetUserSql = @"SELECT COUNT(1) FROM sys.database_principals WHERE name = '{0}'";
        private const string CreateUserSql = @"CREATE USER [{0}] FROM LOGIN [{0}]";

        [ArgPosition(0)]
        [ArgShortcut("na")]
        [ArgDescription("The name of the login")]
        public string Name { get; set; }

        protected override async Task OnExecute()
        {
            var connInfo = await GetSqlConnectionInfo();

            // Connect to master
            if (!WhatIf)
            {
                // Collect list of databases, and grant access to master while we're at it
                IList<string> databases = null;
                using (var connection = await connInfo.Connect("master"))
                {
                    var masterConnStr = new SqlConnectionStringBuilder(connection.ConnectionString);

                    // Make the user a dbmanager
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_CreatingUser,
                        Name,
                        masterConnStr.InitialCatalog));
                    await CreateUserIfNotExists(connection);

                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_ServerManagering,
                        Name,
                        masterConnStr.DataSource));
                    await connection.QueryAsync<int>(
                        "EXEC sp_addrolemember 'dbmanager', '" + Name + "'; " +
                        "EXEC sp_addrolemember 'loginmanager', '" + Name + "';");

                    await Console.WriteInfoLine(Strings.Db_CreateUserCommand_FetchingDBs);
                    databases = (await connection.QueryAsync<string>(@"
                        SELECT name 
                        FROM sys.databases 
                        WHERE name <> 'master'", new { targetDb = connInfo.ConnectionString.InitialCatalog })).ToList();
                    await Console.WriteInfoLine(Strings.Db_CreateUserCommand_RetrievedDatabases, databases.Count);
                }

                Debug.Assert(databases != null);
                // Connect to each Database and make the user a db_owner of that DB
                foreach (var database in databases)
                {
                    using (var connection = await connInfo.Connect(database))
                    {
                        await Console.WriteInfoLine(String.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Db_CreateUserCommand_CreatingUser,
                            Name,
                            database));
                        await CreateUserIfNotExists(connection);

                        await Console.WriteInfoLine(String.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Db_CreateUserCommand_AdminingUser,
                            Name,
                            database));
                        await connection.QueryAsync<int>("EXEC sp_addrolemember 'db_owner', '" + Name + "';");
                    }
                }
            }
        }

        private async Task CreateUserIfNotExists(SqlConnection connection)
        {
            var count = (await connection.QueryAsync<int>(
                                    String.Format(CultureInfo.InvariantCulture, GetUserSql, Name))).SingleOrDefault();
            if (count == 0)
            {
                await connection.QueryAsync<int>(
                    String.Format(CultureInfo.InvariantCulture, CreateUserSql, Name));
            }
        }
    }
}
