using System;
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
            MonitoringSystem.Start();
            
            // Log to ETW
            app.SetLoggerFactory(new DiagnosticsLoggerFactory());
   
            app.UseRequestTracing();
            app.UseWelcomePage();
        }
    }
}
