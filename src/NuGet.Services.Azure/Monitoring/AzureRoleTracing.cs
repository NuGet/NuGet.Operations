using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace NuGet.Services.Monitoring
{
    public static class AzureRoleTracing
    {
        public static void Enable(string instanceName, string storageConnectionString)
        {
            var listener = new AutoAttachObservableEventListener();
            listener.LogToWindowsAzureTable(instanceName, storageConnectionString);
        }
    }
}
