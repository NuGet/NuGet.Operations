using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.SqlServer.Dac;
using PowerArgs;

namespace NuCmd.Commands.Db
{
    [Description("Checks if the specified DAC package needs to be deployed to the target database.")]
    public class CheckDacCommand : DacCommandBase
    {
        [ArgRequired]
        [ArgShortcut("p")]
        [ArgExistingFile]
        [ArgDescription("The DACPAC file built by the build server.")]
        public string DacPac { get; set; }

        protected override async Task OnExecute()
        {
            var connInfo = await GetSqlConnectionInfo();
            var package = DacPackage.Load(DacPac);
            var services = ConnectDac(connInfo);

            await Console.WriteInfoLine(String.Format(
                CultureInfo.CurrentCulture,
                Strings.Db_CheckDacCommand_GettingDeploymentReport,
                package.Name,
                connInfo.ConnectionString.InitialCatalog,
                connInfo.ConnectionString.DataSource));
            var report = services.GenerateDeployReport(package, connInfo.ConnectionString.InitialCatalog);
            var reportDoc = XDocument.Parse(report);
            var ns = XNamespace.Get("http://schemas.microsoft.com/sqlserver/dac/DeployReport/2012/02");
            var alertsElem = reportDoc.Root.Element(ns + "Alerts");
            if (alertsElem != null && alertsElem.Elements().Any())
            {
                throw new InvalidDataException(Strings.Db_CheckDacCommand_DeploymentAlerts);
            }

            var operationsElem = reportDoc.Root.Element(ns + "Operations");
            if (operationsElem == null)
            {
                await Console.WriteInfoLine(Strings.Db_CheckDacCommand_NothingToDeploy);
            }
            else
            {
                await Console.WriteInfoLine(Strings.Db_CheckDacCommand_DeploymentOperations);
                await Console.WriteTable(
                    operationsElem
                        .Elements(ns + "Operation")
                        .SelectMany(operation =>
                            operation.Elements(ns + "Item").Select(item => new
                            {
                                Operation = operation.Attribute("Name").Value,
                                Type = item.Attribute("Type").Value,
                                Name = item.Attribute("Value").Value
                            })));
            }
        }
    }
}
