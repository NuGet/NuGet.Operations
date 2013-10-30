using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Services.Monitoring;
using NuGet.Services.Owin;
using Owin;

[assembly: OwinStartup(typeof(NuGet.Services.Gateway.GatewayService))]

namespace NuGet.Services.Gateway
{
    public class GatewayService
    {
        public void Configuration(IAppBuilder app)
        {
            // Build up the app
            App(app);

            // Configure console tracing
            InteractiveTracing.Enable();
        }

        public static void App(IAppBuilder app)
        {
            // Enable common Owin Middleware
            CommonOwinMiddleware.Attach(app);
            
            // Configure the app-specific stuff
            app.UseWelcomePage();
        }
    }
}
