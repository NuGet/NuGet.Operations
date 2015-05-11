// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.WindowsAzure.Storage;
using NuCmd.Models;
using PowerArgs;

namespace NuCmd.Commands.User
{
    [Description("Renames a user in the primary data center in the target NuGet environment")]
    public class RenameCommand : EnvironmentCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("old")]
        [ArgDescription("The Old Username of the user")]
        public string OldUsername { get; set; }

        [ArgRequired]
        [ArgPosition(1)]
        [ArgShortcut("new")]
        [ArgDescription("The New Username for the user")]
        public string NewUsername { get; set; }

        [ArgRequired]
        [ArgShortcut("r")]
        [ArgDescription("The reason for the user rename. Must be specified.")]
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
                        PackageOwnershipRequests = (SELECT COUNT(*) FROM PackageOwnerRequests WHERE RequestingOwnerKey = u.[Key]),
                        ExistingNewUserEmailAddress = (SELECT IsNull(EmailAddress, UnconfirmedEmailAddress) FROM [Users] newUser WHERE newUser.Username = @NewUsername)

                    FROM [Users] u
                    WHERE u.Username = @OldUsername", new
                    {
                        OldUsername,
                        NewUsername
                    });

                var user = results.SingleOrDefault();

                if (user == null)
                {
                    await Console.WriteErrorLine("Username not found. Aborting.");
                    return;
                }

                if (user.ExistingNewUserEmailAddress != null)
                {
                    await Console.WriteErrorLine(Strings.User_RenameCommand_Error_NewUsernameExists, NewUsername, (string)user.ExistingNewUserEmailAddress);
                    return;
                }

                DateTime createdUtc = ((DateTime?)user.CreatedUtc) ?? DateTime.MinValue;

                await Console.WriteInfoLine(Strings.User_RenameCommand_Confirm_Header, (dc == null ? "<unknown>" : dc.FullName));
                await Console.WriteDataLine(Strings.User_RenameCommand_Confirm_Data, "Username", (string)user.Username);
                await Console.WriteDataLine(Strings.User_RenameCommand_Confirm_Data, "EmailAddress", (string)user.EmailAddress);
                await Console.WriteDataLine(Strings.User_RenameCommand_Confirm_Data, "UnconfirmedEmailAddress", (string)user.UnconfirmedEmailAddress);
                await Console.WriteDataLine(Strings.User_RenameCommand_Confirm_Data, "CreatedUtc", createdUtc.ToString("yyyy/MM/dd HH:mm"));
                await Console.WriteDataLine(Strings.User_RenameCommand_Confirm_Data, "Package Ownerships", (int)user.PackageOwnerships);
                await Console.WriteDataLine(Strings.User_RenameCommand_Confirm_Data, "Package Ownership Invites", (int)user.PackageOwnershipInvites);
                await Console.WriteDataLine(Strings.User_RenameCommand_Confirm_Data, "Package Ownership Requests", (int)user.PackageOwnershipRequests);

                if (!WhatIf)
                {
                    string typed = await Console.Prompt(Strings.User_ConfirmEmailAddress);
                    // If the user has confirmed an email address, then verify that
                    // If the user hasn't confirmed an email address, verify their unconfirmed address
                    if (!String.Equals(typed, user.EmailAddress ?? user.UnconfirmedEmailAddress, StringComparison.Ordinal))
                    {
                        await Console.WriteErrorLine(Strings.User_Error_IncorrectEmailAddress, typed);
                        return;
                    }
                }

                await RenameUser(user, conn);
            }
        }

        private async Task RenameUser(dynamic user, SqlConnection conn)
        {
            var userRecord = await conn.QueryDatatable(
                "SELECT NewUsername = @NewUsername, * FROM [Users] WHERE [Key] = @key",
                new SqlParameter("@key", user.Key), new SqlParameter("@NewUsername", NewUsername));

            var auditRecord = new UserAuditRecord(
                user.Username,
                user.EmailAddress ?? user.UnconfirmedEmailAddress + " (unconfirmed)",
                userRecord,
                UserAuditAction.Renamed,
                Reason);

            await Console.WriteInfoLine(Strings.User_WritingAuditRecord, auditRecord.GetPath());
            if (!WhatIf)
            {
                await auditRecord.WriteAuditRecord("user", StorageAccount);
            }

            await RenameUserRecord(user, conn);

            await Console.WriteInfoLine(Strings.User_RenameCommand_UpdateCompleted);
        }

        private async Task RenameUserRecord(dynamic user, SqlConnection conn)
        {
            await Console.WriteInfoLine(
                Strings.User_RenameCommand_UpdatingUserData,
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

                UPDATE  u
                SET     Username = @NewUsername
                OUTPUT  'Users' AS TableName
                    ,   'Old Username: ' + deleted.Username + '; New Username: ' + inserted.Username AS Value
                INTO    @actions
                FROM    [Users] u
                WHERE   u.Username = @OldUsername

                SELECT  *
                FROM    @actions
                " + (WhatIf ? "ROLLBACK TRAN" : "COMMIT TRAN"), new
                {
                    OldUsername,
                    NewUsername
                });

            await Console.WriteInfoLine(Strings.User_RenameCommand_DatabaseActions);
            await Console.WriteTable(result, d => new
            {
                Action = "UPDATE",
                Table = (string)d.TableName,
                Value = (string)d.Value
            });
        }
    }
}
