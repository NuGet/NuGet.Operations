using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Services.Monitoring;
using NuGet.Services.Owin;
using Owin;

[assembly: OwinStartup(typeof(NuGet.Services.Gateway.Startup))]

namespace NuGet.Services.Gateway
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Build up the app
            BuildApp(app);

            // Configure console tracing
            InteractiveTracing.Enable();
        }

        private static void BuildApp(IAppBuilder app)
        {
            // Enable common Owin Middleware
            CommonOwinMiddleware.Attach(app);
            
            // Configure the app-specific stuff
            app.UseWelcomePage();
        }
    }
}
