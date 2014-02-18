using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using NuGet;
using PowerArgs;

namespace NuCmd.Commands.Package
{
    [Description("Deletes a package from the primary datacenter in the target NuGet environment")]
    public class DeleteCommand : EnvironmentCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("i")]
        [ArgDescription("The ID of the package to delete")]
        public string Id { get; set; }

        [ArgPosition(1)]
        [ArgShortcut("v")]
        [ArgDescription("The Version of the package to delete")]
        public string Version { get; set; }

        [ArgShortcut("a")]
        [ArgDescription("Set this flag to delete all versions of the package.")]
        public bool AllVersions { get; set; }

        [ArgRequired]
        [ArgShortcut("r")]
        [ArgDescription("The reason for deletion. Must be specified.")]
        public string Reason { get; set; }

        [ArgRequired]
        [ArgShortcut("db")]
        [ArgDescription("SQL Connection string for the package database.")]
        public string DatabaseConnectionString { get; set; }

        protected override async Task OnExecute()
        {
            if (String.IsNullOrWhiteSpace(Version) && !AllVersions)
            {
                await Console.WriteErrorLine(Strings.Package_DeleteCommand_AllVersionsRequiredIfVersionNull);
                return;
            }

            // Get Datacenter 0
            var dc = GetDatacenter(0);

            // Parse the version
            var version = SemanticVersion.Parse(Version);

            // Connect to the database
            using (var conn = new SqlConnection(DatabaseConnectionString))
            {
                await conn.OpenAsync();
                var packages = conn.Query(@"
                    SELECT
                        pr.Id,
                        p.NormalizedVersion AS Version, 
                        p.Hash 
                    FROM Packages p
                    INNER JOIN PackageRegistrations pr ON p.PackageRegistrationKey = pr.[Key]
                    WHERE pr.Id = @Id AND (@AllVersions OR p.NormalizedVersion = @Version)", new
                    {
                        Id,
                        AllVersions,
                        Version
                    });

                await Console.WriteInfoLine(Strings.Package_DeleteCommand_DeleteList_Header, dc.FullName);
                foreach (var package in packages)
                {
                    await Console.WriteInfoLine(
                        Strings.Package_DeleteCommand_DeleteList_Item,
                        (string)package.Id,
                        (string)package.Version);
                }
                if (!(await Console.Confirm(Strings.Package_DeleteCommand_DeleteList_Confirm, defaultValue: false)))
                {
                    await Console.WriteInfoLine("Cancelled.");
                }
                else
                {
                    await Console.WriteInfoLine("Continuing.");
                }
            }
        }
    }
}
