using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services;
using NuGet.Services.Operations;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Db
{
    [Description("Generates a new admin password for the specified server and stores it in the secret store")]
    public class GenerateAdminPasswordCommand : DatacenterCommandBase
    {
        [ArgRequired]
        [ArgShortcut("db")]
        [ArgDescription("The type of the SQL Database to create the user on")]
        public KnownSqlConnection Database { get; set; }

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

            // Prep the connection string
            EnsureSession();
            var dc = GetDatacenter();

            // Find the server
            var server = dc.FindResource(ResourceTypes.SqlDb, Database.ToString());
            if (server == null)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DatabaseCommandBase_NoDatabaseInDatacenter,
                    Datacenter.Value,
                    ResourceTypes.SqlDb,
                    Database.ToString()));
            }
            var connStr = new SqlConnectionStringBuilder(server.Value);
            
            // Generate a secret and store it
            string serverName = Utils.GetServerName(connStr.DataSource);
            string secretName = "sqldb." + serverName + ":admin";
            var secrets = await GetEnvironmentSecretStore(Session.CurrentEnvironment);
            if (!WhatIf)
            {
                await secrets.Write(
                    new Secret(
                        new SecretName(secretName),
                        Utils.GeneratePassword(timestamped: true),
                        DateTime.UtcNow,
                        ExpiresAt,
                        SecretType.Password),
                    "nucmd db generateadminpassword");
            }
            await Console.WriteInfoLine(Strings.Db_GenerateAdminPasswordCommand_PasswordGenerated, serverName, secretName);
        }
    }
}
