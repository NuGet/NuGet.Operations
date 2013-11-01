using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.WindowsAzure.ServiceRuntime;
using NuGet.Services.Monitoring;

namespace NuGet.Services
{
    public class ServiceHostRole<TService> : RoleEntryPoint
        where TService : NuGetService, new()
    {
        public override bool OnStart()
        {
            var serviceName = typeof(TService).FullName;
            var instanceId = RoleEnvironment.CurrentRoleInstance.Id;

            // Start the Azure Role tracing system
            AzureRoleTracing.Enable(
                instanceId,
                RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"));

            // Trace start
            ServiceLifetimeEventSource.Log.Starting(
                serviceName,
                instanceId);

            try
            {
                // Set the maximum number of concurrent connections 
                ServicePointManager.DefaultConnectionLimit = 12;

                // Get the HTTP Endpoint
                var ep = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["http"].IPEndpoint;
                var service = new TService();

                WebApp.Start(new StartOptions()
                {
                    Port = ep.Port
                }, service.Build);

                ServiceLifetimeEventSource.Log.Started(
                    serviceName,
                    instanceId);

                return base.OnStart();
            }
            catch (Exception ex)
            {
                ServiceLifetimeEventSource.Log.FailedToStart(
                    serviceName,
                    instanceId,
                    ex.ToString(),
                    ex.StackTrace);
                throw;
            }
        }

        public override void OnStop()
        {
            ServiceLifetimeEventSource.Log.Stop(
                typeof(TService).FullName,
                RoleEnvironment.CurrentRoleInstance.Id);
        }
    }
}
