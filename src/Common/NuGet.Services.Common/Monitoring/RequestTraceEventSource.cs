using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace NuGet.Services.Monitoring
{
    [EventSource(Name = "NuGet-Services-Requests")]
    public sealed class RequestTraceEventSource : EventSource
    {
        // This class is a bit funky because of ETW stuff
        // DO NOT change the order of methods, it is important to ETW

        // Each method should have the form:
        //   public void EventName(...args...) { WriteEvent(N, ... args ...); }
        // Where args are PRIMATIVE types (int, long, string, etc.) and N is the value used in the previous method + 1.

        [Event(
            eventId: 1, 
            Message = "http {1} {2} (ID: {0})", 
            Task = Tasks.HttpRequest,
            Opcode = EventOpcode.Start, 
            Level = EventLevel.Informational)]
        public void StartRequest(string requestId, string method, string url) { WriteEvent(1, requestId, method, url); }

        [Event(
            eventId: 2, 
            Message = "http {1} {2} (ID: {0}, Content Type: {4}, Length: {3})", 
            Task = Tasks.HttpRequest, 
            Opcode = EventOpcode.Stop, 
            Level = EventLevel.Informational)]
        public void EndRequest(string requestId, int statusCode, string reasonPhrase, long contentLength, string contentType) { WriteEvent(2, statusCode, reasonPhrase, contentLength, contentType); }

        public class Tasks
        {
            public const EventTask HttpRequest = (EventTask)0x01;
        }
    }
}
