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
        [ArgDescription("The name of the service the user is for (i.e. 'work', 'search', etc.)")]
        public string Service { get; set; }

        [ArgShortcut("a")]
        [ArgDescription("If set, the user will be an administrator on the database server")]
        public bool Admin { get; set; }

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

            // Connect to master
            var masterConnStr = new SqlConnectionStringBuilder(connStr.ConnectionString);
            masterConnStr.InitialCatalog = "master";
            using(var connection = new SqlConnection(masterConnStr.ConnectionString, cred)) {
                await connection.OpenAsync();
                await Console.WriteInfoLine(String.Format(
                    CultureInfo.CurrentCulture, 
                    Strings.Db_CreateUserCommand_Connected,
                    masterConnStr.DataSource,
                    masterConnStr.InitialCatalog));
            }
        }
    }
}
