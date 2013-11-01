using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Monitoring;

namespace NuGet.Services.Communications
{
    [EventSource(Name = EventSourceNames.ExternalCommunications)]
    public class ExternalCommunicationsEventSource : EventSource
    {
        public static readonly ExternalCommunicationsEventSource Log = new ExternalCommunicationsEventSource();

        private ExternalCommunicationsEventSource() { }

        [Event(
            eventId: 1,
            Message = "Communications with the {0} service are initializing",
            Level = EventLevel.Informational)]
        public void Initialize(string serviceName) { WriteEvent(1, serviceName); }

        [Event(
            eventId: 2,
            Level = EventLevel.Error,
            Message = "Request to {0} failed.\r\nException: {1}\r\nStack Trace: {2}\r\nConcurrent Failures before this: {3}")]
        public void Faulted(string serviceName, string exception, string stackTrace, int failuresBeforeThis) { WriteEvent(2, serviceName, exception, stackTrace, failuresBeforeThis); }

        [Event(
            eventId: 3,
            Level = EventLevel.Error,
            Message = "Request to {0} timed out.\r\nException: {1}\r\nStack Trace: {2}\r\nConcurrent Failures before this: {3}")]
        public void TimedOut(string serviceName, string exception, string stackTrace, int failuresBeforeThis) { WriteEvent(3, serviceName, exception, stackTrace, failuresBeforeThis); }

        [Event(
            eventId: 4,
            Level = EventLevel.Critical,
            Opcode = EventOpcode.Start,
            Task = Tasks.FailFastMode,
            Message = "Communications with the {0} service have failed {1} times in a row. Entering Fail Fast mode.")]
        public void EnteredFailFastFromConsecutiveFailures(string serviceName, int consecutiveFailures) { WriteEvent(4, serviceName, consecutiveFailures); }
        
        [Event(
            eventId: 5,
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Stop,
            Task = Tasks.FailFastMode,
            Message = "Communications with the {0} service have started succeeding again. Leaving Fail Fast mode.")]
        public void LeftFailFast(string serviceName) { WriteEvent(5, serviceName); }

        [Event(
            eventId: 6,
            Level = EventLevel.Warning,
            Message = "The {0} service is in fail fast mode. Request is being terminated.")]
        public void FailFast(string serviceName) { WriteEvent(6, serviceName); }

        [Event(
            eventId: 7,
            Level = EventLevel.Informational,
            Message = "Request to {0} succeeded after {1} consecutive failures")]
        public void SucceededAfterFailures(string serviceName, int consecutiveFailures) { WriteEvent(7, serviceName, consecutiveFailures); }

        [Event(
            eventId: 8,
            Level = EventLevel.Verbose,
            Message = "Request to {0} succeeded.")]
        public void Succeeded(string serviceName) { WriteEvent(8, serviceName); }

        [Event(
            eventId: 9,
            Level = EventLevel.Informational,
            Message = "Service {0} forced into state {1} (#{2}) by explicit request.")]
        public void StateChanged(string serviceName, string stateName, int stateCode) { WriteEvent(9, serviceName, stateName, stateCode); }

        [NonEvent]
        public void StateChanged(string serviceName, FailFastState state) { StateChanged(serviceName, state.ToString(), (int)state); }

        [Event(
            eventId: 10,
            Level = EventLevel.Informational,
            Message = "Service {0} is attempting to leave fail fast mode.")]
        public void LeaveFailFastAttempt(string serviceName) { WriteEvent(10, serviceName); }

        [Event(
            eventId: 11,
            Level = EventLevel.Critical,
            Message = "Service {0} failed to leave fail fast mode.")]
        public void ContinuingFailFast(string serviceName) { WriteEvent(11, serviceName); }

        [Event(
            eventId: 12,
            Level = EventLevel.Informational,
            Message = "Service {0} failure rate is {1}%")]
        public void ReportFailureRate(string serviceName, double failureRate) { WriteEvent(12, serviceName, failureRate); }

        [Event(
            eventId: 12,
            Level = EventLevel.Critical,
            Opcode = EventOpcode.Start,
            Task = Tasks.FailFastMode,
            Message = "Requests to the {0} service have failed {1} in the last {2} minutes. Entering Fail Fast mode.")]
        public void EnteredFailFastFromFailureRate(string serviceName, double rate, double periodInMinutes) { WriteEvent(4, serviceName, rate, periodInMinutes); }
        
        [NonEvent]
        public void EnteredFailFastFromFailureRate(string serviceName, double rate, TimeSpan period) { EnteredFailFastFromFailureRate(serviceName, rate, period.TotalMinutes); }

        [Event(
            eventId: 13,
            Level = EventLevel.Error,
            Message = "Request to {0} failed due to an invalid result.\r\nConcurrent Failures before this: {3}")]
        public void FaultedResult(string serviceName, int failuresBeforeThis) { WriteEvent(13, serviceName, failuresBeforeThis); }

        internal class Tasks
        {
            public const EventTask FailFastMode = (EventTask)0x01;
        }
    }
}
