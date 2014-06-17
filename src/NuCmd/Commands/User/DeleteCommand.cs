using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using NuCmd;
using NuCmd.Models;
using NuGet.Services.Client;
using NuGet.Services.Operations.Model;
using PowerArgs;

namespace NuCmd.Commands.User
{
    [Description("Deletes a user from the primary data center in the target NuGet environment")]
    public class DeleteCommand : EnvironmentCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("u")]
        [ArgDescription("The Username of the user to delete")]
        public string Username { get; set; }

        [ArgRequired]
        [ArgShortcut("r")]
        [ArgDescription("The reason for deletion. Must be specified.")]
        public string Reason { get; set; }

        [ArgShortcut("db")]
        [ArgDescription("SQL Connection string for the package database.")]
        public string DatabaseConnectionString { get; set; }

        [ArgShortcut("st")]
        [ArgDescription("Azure Storage Connection string for the package storage.")]
        public string StorageConnectionString { get; set; }

        private CloudStorageAccount StorageAccount { get; set; }

        protected override async Task OnExecute()
        {
            // Get Datacenter 0
            var dc = GetDatacenter(0, required: false);
            if (dc != null)
            {
                var defaults = await LoadDefaultsFromAzure(dc, DatabaseConnectionString, StorageConnectionString);
                DatabaseConnectionString = defaults.DatabaseConnectionString;
                StorageConnectionString = defaults.StorageConnectionString;
            }

            StorageAccount = CloudStorageAccount.Parse(StorageConnectionString);

            // Connect to the database
            using (var conn = new SqlConnection(DatabaseConnectionString))
            {
                await conn.OpenAsync();
                var results = conn.Query(@"
                    SELECT
                        u.[Key],
                        u.Username,
                        u.EmailAddress,
                        u.UnconfirmedEmailAddress,
                        u.CreatedUtc,
                        PackageOwnerships = (SELECT COUNT(*) FROM PackageRegistrationOwners WHERE UserKey = u.[Key]),
                        PackageOwnershipInvites = (SELECT COUNT(*) FROM PackageOwnerRequests WHERE NewOwnerKey = u.[Key]),
                        PackageOwnershipRequests = (SELECT COUNT(*) FROM PackageOwnerRequests WHERE RequestingOwnerKey = u.[Key])
                    FROM [Users] u
                    WHERE u.Username = @Username", new
                    {
                        Username
                    });

                var user = results.SingleOrDefault();

                if (user == null)
                {
                    await Console.WriteErrorLine("Username not found. Aborting.");
                    return;
                }

                DateTime createdUtc = ((DateTime?)user.CreatedUtc) ?? DateTime.MinValue;

                await Console.WriteInfoLine(Strings.User_DeleteCommand_Confirm_Header, (dc == null ? "<unknown>" : dc.FullName));
                await Console.WriteDataLine(Strings.User_DeleteCommand_Confirm_Data, "Username", (string)user.Username);
                await Console.WriteDataLine(Strings.User_DeleteCommand_Confirm_Data, "EmailAddress", (string)user.EmailAddress);
                await Console.WriteDataLine(Strings.User_DeleteCommand_Confirm_Data, "UnconfirmedEmailAddress", (string)user.UnconfirmedEmailAddress);
                await Console.WriteDataLine(Strings.User_DeleteCommand_Confirm_Data, "CreatedUtc", createdUtc.ToString("yyyy/MM/dd HH:mm"));
                await Console.WriteDataLine(Strings.User_DeleteCommand_Confirm_Data, "Package Ownerships", (int)user.PackageOwnerships);
                await Console.WriteDataLine(Strings.User_DeleteCommand_Confirm_Data, "Package Ownership Invites", (int)user.PackageOwnershipInvites);
                await Console.WriteDataLine(Strings.User_DeleteCommand_Confirm_Data, "Package Ownership Requests", (int)user.PackageOwnershipRequests);

                // Don't allow the user to be deleted if they own any packages
                // But allow deletion if there are pending invites (in either direction)
                if (user.PackageOwnerships > 0)
                {
                    await Console.WriteErrorLine(Strings.User_DeleteCommand_Error_PackageOwnerships, (int)user.PackageOwnerships, (int)user.PackageOwnershipInvites, (int)user.PackageOwnershipRequests);
                    return;
                }

                if (!WhatIf)
                {
                    string typed = await Console.Prompt(Strings.User_DeleteCommand_Confirm);
                    // If the user has confirmed an email address, then verify that
                    // If the user hasn't confirmed an email address, verify their unconfirmed address
                    if (!String.Equals(typed, user.EmailAddress ?? user.UnconfirmedEmailAddress, StringComparison.Ordinal))
                    {
                        await Console.WriteErrorLine(Strings.User_DeleteCommand_Error_IncorrectEmailAddress, typed);
                        return;
                    }
                }

                await DeleteUser(user, conn);
            }
        }

        private async Task DeleteUser(dynamic user, SqlConnection conn)
        {
            var userRecord = await conn.QueryDatatable(
                "SELECT * FROM [Users] WHERE [Key] = @key",
                new SqlParameter("@key", user.Key));

            var auditRecord = new UserAuditRecord(
                user.Username,
                user.EmailAddress ?? user.UnconfirmedEmailAddress + " (unconfirmed)",
                userRecord,
                UserAuditAction.Deleted,
                Reason);

            await Console.WriteInfoLine(Strings.User_WritingAuditRecord, auditRecord.GetPath());
            if (!WhatIf)
            {
                await auditRecord.WriteAuditRecord("user", StorageAccount);
            }

            await DeleteUserData(user, conn);

            await Console.WriteInfoLine(Strings.User_DeleteCommand_DeletionCompleted);
        }

        private async Task DeleteUserData(dynamic user, SqlConnection conn)
        {
            await Console.WriteInfoLine(
                Strings.User_DeleteCommand_DeletingUserData,
                (string)user.Username,
                (string)user.EmailAddress ?? (string)user.UnconfirmedEmailAddress + " (unconfirmed)",
                conn.Database,
                conn.DataSource);

            var result = conn.Query(@"
                BEGIN TRAN

                DECLARE @actions TABLE(
                    TableName nvarchar(50),
                    Value nvarchar(MAX)
                )

                DELETE  por
                OUTPUT  'PackageOwnerRequests' AS TableName,
                        'PackageId: ' + pr.Id + '; New User: ' + nu.Username + '; Requesting User: ' + ru.Username AS Value
                INTO    @actions
                FROM    PackageOwnerRequests por
                JOIN    PackageRegistrations pr ON pr.[Key] = por.PackageRegistrationKey
                JOIN    [Users] nu ON nu.[Key] = por.NewOwnerKey
                JOIN    [Users] ru ON ru.[Key] = por.RequestingOwnerKey
                WHERE   @key IN (nu.[Key], ru.[Key])

                DELETE  u
                OUTPUT  'Users' AS TableName,
                        'Username: ' + deleted.Username AS Value
                INTO    @actions
                FROM    [Users] u
                WHERE   u.[Key] = @key

                SELECT  *
                FROM    @actions
                " + (WhatIf ? "ROLLBACK TRAN" : "COMMIT TRAN"), new
                {
                    key = (int)user.Key
                });

            await Console.WriteInfoLine(Strings.User_DeleteCommand_DatabaseActions);
            await Console.WriteTable(result, d => new
            {
                Action = "DELETE",
                Table = (string)d.TableName,
                Value = (string)d.Value
            });
        }
    }
}
