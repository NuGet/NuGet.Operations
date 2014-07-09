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
    [Description("Regenerates the password for the specified database user")]
    public class SetPasswordCommand : DatabaseCommandBase
    {
        [ArgPosition(0)]
        [ArgShortcut("na")]
        [ArgDescription("The name of the user")]
        public string Name { get; set; }

        [ArgShortcut("pass")]
        [ArgDescription("The password to assign to the user. If omitted, a password will be generated and placed in the secret store")]
        public string Password { get; set; }

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

            // Generate a password
            if (String.IsNullOrEmpty(Password))
            {
                Password = Utils.GeneratePassword(timestamped: false);
            }

            // Test connection to Secret Store
            //  We have a current environment because GetSqlConnectionInfo ensures that one exists
            //  GetEnvironmentSecretStore will throw if the store does not exist
            var secrets = await GetEnvironmentSecretStore(Session.CurrentEnvironment);

            // Connect to master
            if (!WhatIf)
            {
                using (var connection = await connInfo.Connect("master"))
                {
                    var masterConnStr = new SqlConnectionStringBuilder(connection.ConnectionString);
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_CreateUserCommand_Connected,
                        masterConnStr.DataSource,
                        masterConnStr.InitialCatalog));

                    // Reset the password
                    await Console.WriteInfoLine(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_NewPasswordCommand_ResettingPassword,
                        Name));
                    await connection.QueryAsync<int>("ALTER LOGIN [" + Name + "] WITH password='" + Password + "'");
                }

                // Generate the connection string
                var loginConnStr = new SqlConnectionStringBuilder(connInfo.ConnectionString.ConnectionString)
                {
                    UserID = Name,
                    Password = Password,
                    ConnectTimeout = 30,
                    Encrypt = true,
                    IntegratedSecurity = false
                };

                // Save the connection string
                string serverBaseName = "sqldb." + connInfo.GetServerName();
                var secretName = new SecretName(serverBaseName + ":logins." + Name);
                await Console.WriteInfoLine(Strings.Db_CreateUserCommand_SavingConnectionString, secretName.Name);
                await secrets.Write(new Secret(
                    secretName,
                    loginConnStr.ConnectionString,
                    DateTime.UtcNow,
                    ExpiresAt,
                    SecretType.Password),
                    "nucmd db newpassword");
            }
            else
            {
                await Console.WriteInfoLine(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_NewPasswordCommand_WouldResetPassword,
                    connInfo.ConnectionString.DataSource,
                    connInfo.ConnectionString.InitialCatalog,
                    Name));
            }
        }
    }
}
