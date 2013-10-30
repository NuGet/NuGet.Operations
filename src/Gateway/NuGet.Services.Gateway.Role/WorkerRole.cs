using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Owin.Hosting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using NuGet.Services.Monitoring;

namespace NuGet.Services.Gateway.Role
{
    public class WorkerRole : RoleEntryPoint
    {
        public override void Run()
        {
            Trace.TraceInformation("WebApiRole entry point called", "Information");

            while (true)
            {
                Thread.Sleep(10000);
                Trace.TraceInformation("Working", "Information");
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Start the Azure Role tracing system
            AzureRoleTracing.Enable(
                RoleEnvironment.CurrentRoleInstance.Id,
                RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"));

            // Get the HTTP Endpoint
            var ep = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["http"].IPEndpoint;

            WebApp.Start(new StartOptions()
            {
                Port = ep.Port
            }, GatewayService.App);

            return base.OnStart();
        }
    }
}
