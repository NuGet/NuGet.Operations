using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Logging;
using NuGet.Services.Monitoring;
using Owin;

namespace NuGet.Services
{
    public abstract class NuGetService
    {
        /// <summary>
        /// Builds the app with NO additional hosting infrastructure configured (tracing output, etc.).
        /// Used for hosting the app within a worker role, etc.
        /// </summary>
        /// <param name="app"></param>
        public virtual void Build(IAppBuilder app)
        {
            // Trace to ETW
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new EventProviderTraceListener(EventSourceNames.SystemDiagnosticsProviderId, EventSourceNames.SystemDiagnostics));

            // Log to System.Diagnostics.Trace
            app.SetLoggerFactory(new DiagnosticsLoggerFactory());

            // Trace requests
            app.UseRequestTracing(GetType().FullName);

            BuildService(app);
        }

        /// <summary>
        /// Default bootstrap method for "interactive" Owin Hosting
        /// </summary>
        /// <param name="app"></param>
        public virtual void Configuration(IAppBuilder app)
        {
            Build(app);
            InteractiveTracing.Enable();
        }

        protected abstract void BuildService(IAppBuilder app);
    }
}