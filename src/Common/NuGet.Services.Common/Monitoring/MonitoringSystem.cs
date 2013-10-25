using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace NuGet.Services.Monitoring
{
    public static class MonitoringSystem
    {
        public static readonly Guid NetFxTraceProviderId = Guid.Parse("{6CC8C9C3-1B86-44DB-A651-C020C3073720}");

        public static void Start()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new EventProviderTraceListener(NetFxTraceProviderId.ToString(), "NuGet-NetFxTrace"));
        }
    }
}
