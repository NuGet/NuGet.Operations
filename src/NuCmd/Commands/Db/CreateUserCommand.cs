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
using System.ComponentModel;
using System.Threading;
using NuGet.Services.Operations.Secrets;
using System.Text.RegularExpressions;
using NuGet.Services.Operations;
using System.Diagnostics;

namespace NuCmd.Commands.Db
{
    [Description("Creates a new SQL user for use by the specified service and with access to the specified schemas")]
    public class CreateUserCommand : DatabaseCommandBase
    {
        [ArgPosition(0)]
        [ArgShortcut("na")]
        [ArgRegex("[0-9a-zA-Z_]+")]
        [ArgDescription("The name of the user")]
        public string Name { get; set; }

        [ArgShortcut("s")]
        [ArgDescription("Comma-separated list of DB schemas to grant access for. See 'nucmd db schemas' for a list.")]
        public string[] Schemas { get; set; }

        [ArgShortcut("sa")]
        [ArgDescription("If set, the user will be an administrator (dbmanager, loginmanager) on the database server")]
        public bool ServerAdmin { get; set; }

        [ArgShortcut("xin")]
        [ArgDescription("Sets the expiry date of the secret to the current date, plus this time")]
        public TimeSpan? ExpiresIn { get; set; }

        [ArgShortcut("xat")]
        [ArgDescription("Sets the expiry date of the secret to the provided date, in local time.")]
        public DateTime? ExpiresAt { get; set; }

        protected override async Task OnExecute()
        {
            if (ExpiresIn != null)
            {
                ExpiresAt = DateTime.Now + ExpiresIn.Value;
            }
            if (ExpiresAt != null)
            {
                ExpiresAt = ExpiresAt.Value.ToUniversalTime();
            }
            else 
            {
                ExpiresAt = DateTime.UtcNow.AddDays(14); // Two week expiration by default
            }

            var connInfo = await GetSqlConnectionInfo();
            
            // Generate the login name
            string loginName = Name.ToLowerInvariant() + "_" + DateTime.UtcNow.ToString("yyyyMMMdd");
            
            // Generate a password
            string loginPassword = Utils.GeneratePassword(timestamped: false);

            // Test connection to Secret Store
            //  We have a current environment because GetSqlConnectionInfo ensures that one exists
            //  GetEnvironmentSecretStore will throw if the store does not exist
            var secrets = await GetEnvironmentSecretStore(Session.CurrentEnvironment);

            // Connect to master
            if (!WhatIf)
            {
                IList<string> databases = null;
                using (var connection = await connInfo.Connect("master"))
                {
                    var masterConnStr = new SqlConnectionStringBuilder(connection.ConnectionString);
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_Connected,
                        masterConnStr.DataSource,
                        masterConnStr.InitialCatalog));

                    // Create the login.\
                    // Can't use SQL Parameters here unfortunately. But the risk is low:
                    //  1. This is an admin/operations tool, only our administrators will use it
                    //  2. We use a Regex to restrict the Service name and then we derive the login name from that using only safe characters
                    //  3. The password is also derived from safe characters
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_CreatingLogin,
                        loginName,
                        masterConnStr.DataSource));
                    await connection.QueryAsync<int>("CREATE LOGIN [" + loginName + "] WITH password='" + loginPassword + "'");

                    if (ServerAdmin)
                    {
                        // Make the user a dbmanager
                        await Console.WriteInfoLine(String.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Db_CreateUserCommand_CreatingUser,
                            loginName,
                            masterConnStr.InitialCatalog));
                        await connection.QueryAsync<int>(
                            "CREATE USER [" + loginName + "] FROM LOGIN [" + loginName + "]");

                        await Console.WriteInfoLine(String.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Db_CreateUserCommand_ServerManagering,
                            loginName,
                            masterConnStr.DataSource));
                        await connection.QueryAsync<int>(
                            "EXEC sp_addrolemember 'dbmanager', '" + loginName + "'; " +
                            "EXEC sp_addrolemember 'loginmanager', '" + loginName + "';");

                        await Console.WriteInfoLine(Strings.Db_CreateUserCommand_FetchingDBs);
                        databases = (await connection.QueryAsync<string>(@"
                            SELECT name 
                            FROM sys.databases 
                            WHERE name <> 'master' 
                            AND name <> @targetDb", new { targetDb = connInfo.ConnectionString.InitialCatalog })).ToList();
                        await Console.WriteInfoLine(Strings.Db_CreateUserCommand_RetrievedDatabases, databases.Count);
                    }
                }

                if(ServerAdmin)
                {
                    Debug.Assert(databases != null);
                    // Connect to each Database except for the target db and master and make the user a db_owner of that DB
                    foreach (var database in databases)
                    {
                        using(var connection = await connInfo.Connect(database))
                        {
                            await Console.WriteInfoLine(String.Format(
                                CultureInfo.CurrentCulture,
                                Strings.Db_CreateUserCommand_CreatingUser,
                                loginName,
                                database));
                            await connection.QueryAsync<int>("CREATE USER [" + loginName + "] FROM LOGIN [" + loginName + "]");

                            await Console.WriteInfoLine(String.Format(
                                CultureInfo.CurrentCulture,
                                Strings.Db_CreateUserCommand_AdminingUser,
                                loginName,
                                database));
                            await connection.QueryAsync<int>("EXEC sp_addrolemember 'db_owner', '" + loginName + "';");
                        }
                    }
                }

                // Connect to the database itself
                using (var connection = await connInfo.Connect())
                {
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_Connected,
                        connInfo.ConnectionString.DataSource,
                        connInfo.ConnectionString.InitialCatalog));

                    // Create the user and grant permissions
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_CreatingUser,
                        loginName,
                        connInfo.ConnectionString.InitialCatalog));
                    await connection.QueryAsync<int>(
                        "CREATE USER [" + loginName + "] FROM LOGIN [" + loginName + "]");

                    if (Schemas == null)
                    {
                        await Console.WriteWarningLine(Strings.Db_CreateUserCommand_NoSchemasSpecified);
                        Schemas = new [] { "dbo" };
                    }

                    foreach (var schema in Schemas)
                    {
                        await Console.WriteInfoLine(String.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Db_CreateUserCommand_GrantingUser,
                            loginName,
                            schema,
                            connInfo.ConnectionString.InitialCatalog));
                        await connection.QueryAsync<int>(
                            "GRANT CONTROL ON SCHEMA :: [" + schema + "] TO " + loginName);
                    }
                }

                // Save the password
                string serverBaseName = "sql." + Database.ToString().ToLowerInvariant();
                var secretName = new SecretName(serverBaseName + ":logins." + loginName);
                await Console.WriteInfoLine(Strings.Db_CreateUserCommand_SavingConnectionString, secretName.Name);
                await secrets.Write(new Secret(
                    secretName,
                    loginPassword,
                    DateTime.UtcNow,
                    ExpiresAt,
                    SecretType.Password),
                    "nucmd db createuser");

                // Save a link to the full user connection without the timestamp
                string latestUserSecretName = serverBaseName + ":users." + Name;
                await secrets.Write(new Secret(
                    new SecretName(latestUserSecretName),
                    secretName.ToString(),
                    DateTime.UtcNow,
                    ExpiresAt,
                    SecretType.Link),
                    "nucmd db createuser");
            }
            else
            {
                await Console.WriteInfoLine(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_CreateUserCommand_WouldCreateUser,
                    connInfo.ConnectionString.DataSource,
                    connInfo.ConnectionString.InitialCatalog,
                    loginName));
            }
        }
    }
}
