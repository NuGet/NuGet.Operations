using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace NuGet.Services.Monitoring
{
    public static class AzureRoleTracing
    {
        public static class TableNames
        {
            public static readonly string Prefix = "NuGetTrace";
            public static readonly string ServiceActivity = Prefix + "ServiceActivity";
        }

        public static void Enable(string instanceName, string storageConnectionString)
        {
            // Set up NuGetTraceServiceActivity table
            var listener = WindowsAzureTableLog.CreateListener(
                instanceName,
                storageConnectionString,
                tableAddress: TableNames.ServiceActivity);
            listener.EnableEvents(HttpTraceEventSource.Log, EventLevel.LogAlways);
            listener.EnableEvents(ServiceLifetimeEventSource.Log, EventLevel.LogAlways);
        }
    }
}
