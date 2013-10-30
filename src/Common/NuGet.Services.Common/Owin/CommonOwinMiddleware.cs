using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin.Logging;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using NuGet.Services.Monitoring;

namespace NuGet.Services.Owin
{
    public static class CommonOwinMiddleware
    {
        public static void Attach(IAppBuilder app)
        {
            // Trace to ETW
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new EventProviderTraceListener(EventSourceNames.NetFxTraceProviderId, EventSourceNames.NetFxTrace));

            // Log to System.Diagnostics.Trace
            app.SetLoggerFactory(new DiagnosticsLoggerFactory());

            // Trace requests
            app.UseRequestTracing();
        }
    }
}
