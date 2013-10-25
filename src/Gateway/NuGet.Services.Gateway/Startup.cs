using System;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using NuGet.Services.Monitoring;
using Owin;

[assembly: OwinStartup(typeof(NuGet.Services.Gateway.Startup))]

namespace NuGet.Services.Gateway
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure ETW
            MonitoringSystem.ConfigureDefault();

            // Log to ETW
            app.SetLoggerFactory(new DiagnosticsLoggerFactory());
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new EventProviderTraceListener("NuGet-NetFxTrace"));
   
            app.UseRequestTracing();
            app.UseWelcomePage();
        }
    }
}
