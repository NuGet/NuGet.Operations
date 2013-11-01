using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Monitoring
{
    [EventSource(Name = EventSourceNames.ServiceLifetime)]
    public class ServiceLifetimeEventSource : EventSource
    {
        public static readonly ServiceLifetimeEventSource Log = new ServiceLifetimeEventSource();

        private ServiceLifetimeEventSource() { }

        /// <summary>
        /// Event raised when a service is starting up
        /// </summary>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="instanceName">The id of the service instance</param>
        [Event(
            eventId: 1, 
            Message = "Service {0}, instance {1}: Starting",
            Level = EventLevel.Informational,
            Task = Tasks.Service)]
        public void Starting(string serviceName, string instanceName) { WriteEvent(1, serviceName, instanceName); }

        /// <summary>
        /// Event raised when a service has started
        /// </summary>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="instanceName">The id of the service instance</param>
        [Event(
            eventId: 2,
            Message = "Service {0}, instance {1}: Started",
            Opcode = EventOpcode.Start,
            Level = EventLevel.Informational,
            Task = Tasks.Service)]
        public void Started(string serviceName, string instanceName) { WriteEvent(2, serviceName, instanceName); }

        /// <summary>
        /// Event raised when a service has stopped
        /// </summary>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="instanceName">The id of the service instance</param>
        [Event(
            eventId: 3,
            Message = "Service {0}, instance {1}: Stopped",
            Opcode = EventOpcode.Stop,
            Level = EventLevel.Informational,
            Task = Tasks.Service)]
        public void Stop(string serviceName, string instanceName) { WriteEvent(3, serviceName, instanceName); }

        /// <summary>
        /// Event raised when a service has failed to start
        /// </summary>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="instanceName">The id of the service instance</param>
        /// <param name="exception">A dump of the Exception data</param>
        /// <param name="stackTrace">The stack trace of the Exception</param>
        [Event(
            eventId: 4,
            Message = "Service {0}, instance {1}: Failed to Start.\r\nException: {2}.\r\nStack Trace: {3}",
            Opcode = EventOpcode.Stop,
            Level = EventLevel.Critical,
            Task = Tasks.Service)]
        public void FailedToStart(string serviceName, string instanceName, string exception, string stackTrace) { WriteEvent(4, serviceName, instanceName, exception, stackTrace); }

        internal static class Tasks {
            public const EventTask Service = (EventTask)0x01;
        }
    }
}
