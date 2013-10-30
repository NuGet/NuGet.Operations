using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Observable;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Schema;

namespace NuGet.Services.Monitoring
{
    public class AutoAttachObservableEventListener : EventListener, IObservable<EventEntry>
    {
        // Most of this class is borrowed from: https://entlib.codeplex.com/SourceControl/latest#Blocks/SemanticLogging/Src/SemanticLogging/ObservableEventListener.cs
        // Used under Ms-PL

        private EventSourceSchemaCache schemaCache = EventSourceSchemaCache.Instance;
        private EventEntrySubject subject = new EventEntrySubject();

        /// <summary>
        /// Releases all resources used by the current instance and unsubscribes all the observers.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Incorrect implementation is inherited from base class")]
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Calls the base class Dispose() and the local class Dispose(bool)")]
        public override void Dispose()
        {
            base.Dispose();
            this.subject.Dispose();
        }

        /// <summary>
        /// Called whenever an event has been written by an event source for which the event listener has enabled events.
        /// </summary>
        /// <param name="eventData">The event arguments that describe the event.</param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            EventSchema schema = null;
            try
            {
                schema = this.schemaCache.GetSchema(eventData.EventId, eventData.EventSource);
            }
            catch (Exception ex)
            {
                // Not sure what to do with this since this event is internal to SLAB...
                //SemanticLoggingEventSource.Log.ParsingEventSourceManifestFailed(eventData.EventSource.Name, eventData.EventId, ex.ToString());
                return;
            }

            var entry = EventEntry.Create(eventData, schema);

            this.subject.OnNext(entry);
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications
        /// before the provider has finished sending them.</returns>
        public IDisposable Subscribe(IObserver<EventEntry> observer)
        {
            return this.subject.Subscribe(observer);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            EnableEvents(eventSource, EventLevel.LogAlways);
        }
    }
}
