using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NuGet.Services.Operations;
using PowerArgs;

namespace NuCmd.Commands.Db
{
    [Description("Gets a SQL Connection String for the specified database")]
    public class GetCsCommand : DatabaseCommandBase
    {
        [ArgDescription("The base user name to look up the connection string for. Defaults to the same name as the database")]
        [ArgShortcut("bu")]
        public string BaseUserName { get; set; }

        [ArgDescription("Set this switch to get the admin credential for the database. Overrides BaseUserName")]
        [ArgShortcut("a")]
        public bool Admin { get; set; }

        [ArgDescription("Set this switch to copy the value to the clipboard instead of displaying it")]
        public bool Clip { get; set; }

        protected override async Task OnExecute()
        {
            BaseUserName = String.IsNullOrEmpty(BaseUserName) ?
                Database.ToString().ToLower() :
                BaseUserName;

            // Get the datacenter
            var dc = GetDatacenter(Datacenter ?? 0, required: true);

            // Find the server
            var server = dc.FindResource(ResourceTypes.SqlDb, Database.ToString());
            if (server == null)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DatabaseCommandBase_NoDatabaseInDatacenter,
                    Datacenter,
                    ResourceTypes.SqlDb,
                    Database.ToString()));
            }
            var appModelCstr = new SqlConnectionStringBuilder(server.Value);

            // Look up the current connection string
            var secretName = "sqldb." + Utils.GetServerName(server) + (
                Admin ?
                    ":admin" :
                    (":users." + BaseUserName.ToLower()));
            var value = await GetSecretOrDefault(secretName);
            if (String.IsNullOrEmpty(value))
            {
                await Console.WriteErrorLine(Strings.Secrets_NoSuchSecret, secretName);
                return;
            }

            // Build the connection string
            string finalString = null;
            if (Admin)
            {
                appModelCstr.UserID = Utils.GetAdminUserName(server, dc);
                appModelCstr.Password = value;
                appModelCstr.ConnectTimeout = 30;
                appModelCstr.Encrypt = true;
                appModelCstr.IntegratedSecurity = false;
                finalString = appModelCstr.ConnectionString;
            }
            else
            {
                var cstr = new SqlConnectionStringBuilder(value);
                cstr.InitialCatalog = appModelCstr.InitialCatalog;
                finalString = cstr.ConnectionString;
            }

            // Display or Copy the name
            if (Clip)
            {
                await STAHelper.InSTAThread(() => Clipboard.SetText(finalString));
                await Console.WriteInfoLine(Strings.Db_GetCsCommand_ConnectionStringCopied);
            }
            else
            {
                await Console.WriteDataLine(finalString);
            }
        }
    }
}
