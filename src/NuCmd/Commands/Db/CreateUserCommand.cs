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

namespace NuCmd.Commands.Db
{
    public class CreateUserCommand : DatacenterCommandBase
    {
        [ArgRequired]
        [ArgShortcut("db")]
        [ArgDescription("The type of the SQL Database to create the user on")]
        public KnownSqlConnection Database { get; set; }

        [ArgRequired]
        [ArgShortcut("au")]
        [ArgDescription("The Admin User Name for the database")]
        public string AdminUser { get; set; }

        [ArgShortcut("s")]
        [ArgRegex("[0-9a-zA-Z]+")]
        [ArgDescription("The name of the service the user is for (i.e. 'work', 'search', etc.)")]
        public string Service { get; set; }

        [ArgShortcut("sa")]
        [ArgDescription("If set, the user will be an administrator on the database server")]
        public bool ServerAdmin { get; set; }

        [ArgShortcut("c")]
        [ArgDescription("If set, the connection string for the service will be put in the clipboard")]
        public bool Clip { get; set; }

        protected override async Task OnExecute()
        {
            // Prep the connection string
            EnsureSession();
            var dc = GetDatacenter();
                
            // Find the server
            var server = dc.FindResource(ResourceTypes.SqlDb, Database.ToString());
            if(server == null) {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture, 
                    Strings.Db_CreateUserCommand_NoDatabaseInDatacenter,
                    Datacenter.Value,
                    ResourceTypes.SqlDb,
                    Database.ToString()));
            }
            var connStr = new SqlConnectionStringBuilder(server.Value);
            if (String.IsNullOrEmpty(connStr.InitialCatalog))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_CreateUserCommand_ResourceMissingRequiredConnectionStringField,
                    ResourceTypes.SqlDb,
                    server.Name,
                    "InitialCatalog"));
            }
            if (String.IsNullOrEmpty(connStr.DataSource))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_CreateUserCommand_ResourceMissingRequiredConnectionStringField,
                    ResourceTypes.SqlDb,
                    server.Name,
                    "DataSource"));
            }
            if(!String.IsNullOrEmpty(connStr.UserID)) {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_CreateUserCommand_ResourceHasUnexpectedConnectionStringField,
                    ResourceTypes.SqlDb,
                    server.Name,
                    "User ID"));
            }
            if(!String.IsNullOrEmpty(connStr.Password)) {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_CreateUserCommand_ResourceHasUnexpectedConnectionStringField,
                    ResourceTypes.SqlDb,
                    server.Name,
                    "Password"));
            }

            // Prompt the user for the admin password and put it in a SecureString
            var password = Console.PromptForPassword(String.Format(
                CultureInfo.CurrentCulture, 
                Strings.Db_CreateUserCommand_EnterAdminPassword,
                AdminUser));

            // Create a SQL Credential
            var cred = new SqlCredential(AdminUser, password);

            // Generate the login name
            string loginName = Service.ToLowerInvariant() + "_" + DateTime.UtcNow.ToString("yyyyMMMdd");

            // Generate a password
            string loginPassword =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        Guid.NewGuid().ToString("N")));

            // Connect to master
            if (!WhatIf)
            {
                var masterConnStr = new SqlConnectionStringBuilder(connStr.ConnectionString);
                masterConnStr.InitialCatalog = "master";
                using (var connection = new SqlConnection(masterConnStr.ConnectionString, cred))
                {
                    await connection.OpenAsync();
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_Connected,
                        masterConnStr.DataSource,
                        masterConnStr.InitialCatalog));

                    // Create the login
                    // Can't use SQL Parameters here unfortunately. But the risk is low:
                    //  1. This is an admin/operations tool, only our administrators will use it
                    //  2. We use a Regex to restrict the Service name and then we derive the login name from that using only safe characters
                    //  3. The password is also derived from safe characters
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_CreatingLogin,
                        loginName,
                        connStr.DataSource));
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
                            Strings.Db_CreateUserCommand_AdminingUser,
                            loginName,
                            masterConnStr.DataSource));
                        await connection.QueryAsync<int>(
                            "EXEC sp_addrolemember 'dbmanager', '" + loginName + "'; " +
                            "EXEC sp_addrolemember 'loginmanager', '" + loginName + "';");
                    }
                }

                // Connect to the database itself
                using (var connection = new SqlConnection(connStr.ConnectionString, cred))
                {
                    await connection.OpenAsync();
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_Connected,
                        connStr.DataSource,
                        connStr.InitialCatalog));

                    // Create the user and grant permissions
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_CreatingUser,
                        loginName,
                        connStr.InitialCatalog));
                    await connection.QueryAsync<int>(
                        "CREATE USER [" + loginName + "] FROM LOGIN [" + loginName + "]");
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_GrantingUser,
                        loginName,
                        Service,
                        connStr.InitialCatalog));
                    await connection.QueryAsync<int>(
                        "GRANT CONTROL ON SCHEMA :: " + Service + " TO " + loginName);
                }

                // Generate the connection string
                var loginConnStr = new SqlConnectionStringBuilder(connStr.ConnectionString)
                {
                    UserID = loginName,
                    Password = loginPassword
                };

                if (Clip)
                {
                    Clipboard.SetText(loginConnStr.ConnectionString);
                    await Console.WriteInfoLine("");
                }
                else
                {
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_CreatedUser,
                        loginName,
                        loginPassword,
                        Service,
                        loginConnStr.ConnectionString));
                }
            }
            else
            {
                await Console.WriteInfoLine(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_CreateUserCommand_WouldCreateUser,
                    connStr.DataSource,
                    connStr.InitialCatalog,
                    Service));
            }
        }
    }
}
