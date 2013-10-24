using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Services.Monitoring;
using Owin;

[assembly: OwinStartup(typeof(NuGet.Services.Gateway.Startup))]

namespace NuGet.Services.Gateway
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var traceEventSource = new RequestTraceEventSource();

            app.UseTracing(traceEventSource);
            app.UseWelcomePage();
        }
    }
}
