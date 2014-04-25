using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PowerArgs;
using Dapper;

namespace NuCmd.Commands.Db
{
    [Description("Sanitizes personal information from the specified Legacy database backup")]
    public class SanitizeCommand : DatabaseCommandBase
    {
        private static readonly Regex BackupOrExportMatcher = new Regex("(Backup|Export)_.*");

        private const string SanitizeUsersQuery = @"
            UPDATE Users
            SET    ApiKey = NEWID(),
                    EmailAddress = [Username] + '@' + @EmailDomain,
                    UnconfirmedEmailAddress = NULL,
                    HashedPassword = CAST(NEWID() AS NVARCHAR(MAX)),
                    EmailAllowed = 1,
                    EmailConfirmationToken = NULL,
                    PasswordResetToken = NULL,
                    PasswordResetTokenExpirationDate = NULL,
                    PasswordHashAlgorithm = 'PBKDF2'
            WHERE   [Key] NOT IN (SELECT ur.UserKey FROM UserRoles ur INNER JOIN Roles r ON r.[Key] = ur.RoleKey WHERE r.Name = 'Admins')";

        [ArgRequired]
        [ArgShortcut("b")]
        [ArgDescription("The name of the backup to sanitize")]
        public string BackupName { get; set; }

        [ArgShortcut("e")]
        [ArgDescription("Domain name to use for sanitized email addresses, username@[emaildomain]")]
        public string EmailDomain { get; set; }

        protected override async Task OnExecute()
        {
            var connInfo = await GetSqlConnectionInfo();
            if (!BackupOrExportMatcher.IsMatch(BackupName))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_SanitizeCommand_CannotSanitize,
                    BackupName));
            }

            EmailDomain = EmailDomain ?? "nugettest.org";

            await Console.WriteInfoLine(Strings.Db_SanitizeCommand_Sanitizing, connInfo.ConnectionString.DataSource, connInfo.ConnectionString.InitialCatalog);
            if (!WhatIf)
            {
                using (var connection = await connInfo.Connect(BackupName))
                {
                    await connection.QueryAsync<int>(SanitizeUsersQuery, new { EmailDomain });
                }
            }
            await Console.WriteInfoLine(Strings.Db_SanitizeCommand_Sanitized, connInfo.ConnectionString.DataSource, connInfo.ConnectionString.InitialCatalog);
        }
    }
}
