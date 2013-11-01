using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace NuGet.Services.Monitoring
{
    [EventSource(Name = EventSourceNames.Http)]
    public sealed class HttpTraceEventSource : EventSource
    {
        public static readonly HttpTraceEventSource Log = new HttpTraceEventSource();

        private HttpTraceEventSource() { }

        // This class is a bit funky because of ETW stuff
        // DO NOT change the order of methods, it is important to ETW

        // Each method should have the form:
        //   public void EventName(...args...) { WriteEvent(N, ... args ...); }
        // Where args are PRIMATIVE types (int, long, string, etc.) and N is the value used in the previous method + 1.

        /// <summary>
        /// Event raised when a request is received
        /// </summary>
        /// <param name="requestId">The unique id assigned to the request</param>
        /// <param name="serviceName">The name of the service that is currently processing the request</param>
        /// <param name="method">The HTTP Method of the incoming request</param>
        /// <param name="url">The URL of the incoming request</param>
        [Event(
            eventId: 1, 
            Message = "-> http {2} {3} (ID: {0})", 
            Task = Tasks.HttpRequest,
            Opcode = EventOpcode.Receive, 
            Level = EventLevel.Informational)]
        public void Received(string requestId, string serviceName, string method, string url) { WriteEvent(1, requestId, serviceName, method, url); }

        /// <summary>
        /// Event raised when a response is prepared and is about to be sent
        /// </summary>
        /// <param name="requestId">The unique id assigned to the request</param>
        /// <param name="serviceName">The name of the service that is currently processing the request</param>
        /// <param name="statusCode">The HTTP Status Code of the response</param>
        /// <param name="reasonPhrase">The HTTP Reason Phrase of the response</param>
        /// <param name="contentLength">The HTTP Content Length of the response</param>
        /// <param name="contentType">The HTTP Content Type of the response</param>
        [Event(
            eventId: 2, 
            Message = "<- http {2} {3} (ID: {0})", 
            Task = Tasks.HttpRequest, 
            Opcode = EventOpcode.Reply, 
            Level = EventLevel.Informational)]
        public void Responding(string requestId, string serviceName, int statusCode, string reasonPhrase, long contentLength, string contentType) { WriteEvent(2, requestId, serviceName, statusCode, reasonPhrase ?? String.Empty, contentLength, contentType ?? String.Empty); }

        /// <summary>
        /// Event raised when an unhandled exception occurs in the HTTP pipeline
        /// </summary>
        /// <param name="requestId">The unique id assigned to the request</param>
        /// <param name="serviceName">The name of the service that is currently processing the request</param>
        /// <param name="statusCode">The HTTP Status Code of the response</param>
        /// <param name="reasonPhrase">The HTTP Reason Phrase of the response</param>
        /// <param name="exception">A dump of the Exception data</param>
        /// <param name="stackTrace">The stack trace of the Exception</param>
        [Event(
            eventId: 3,
            Message = "!! http {2} {3}\r\nException: {4}\r\nStack Trace: {5}",
            Task = Tasks.HttpRequest,
            Level = EventLevel.Error)]
        public void Faulted(string requestId, string serviceName, int statusCode, string reasonPhrase, string exception, string stackTrace) { WriteEvent(3, requestId, serviceName, statusCode, reasonPhrase ?? String.Empty, exception, stackTrace); }

        public class Tasks
        {
            public const EventTask HttpRequest = (EventTask)0x01;
        }
    }
}
