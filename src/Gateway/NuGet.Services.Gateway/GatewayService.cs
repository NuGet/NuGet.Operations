using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Services.Monitoring;
using Owin;

[assembly: OwinStartup(typeof(NuGet.Services.Gateway.GatewayService))]

namespace NuGet.Services.Gateway
{
    public class GatewayService : NuGetService
    {
        protected override void BuildService(IAppBuilder app)
        {
            app.UseWelcomePage();
        }
    }
}
