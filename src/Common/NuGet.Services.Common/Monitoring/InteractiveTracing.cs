using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;

namespace NuGet.Services.Monitoring
{
    public static class InteractiveTracing
    {
        [Conditional("TRACE")]
        public static void Enable()
        {
            // Check if the tracing has been disabled
            string traceDisabledValue = Environment.GetEnvironmentVariable("NUGET_TRACE_DISABLE");
            bool disabled = false;
            if(!String.IsNullOrEmpty(traceDisabledValue) && !Boolean.TryParse(traceDisabledValue, out disabled))
            {
                Trace.WriteLine("Unable to parse NUGET_TRACE_DISABLE value: " + traceDisabledValue);
            }

            if (disabled)
            {
                return;
            }

            // Set up the event listener
            var listener = new AutoAttachObservableEventListener();

            // Enable live tracing to console
            listener.LogToConsole(new Formatter(), new ColorMapper());
        }

        internal class ColorMapper : IConsoleColorMapper
        {
            private static Dictionary<EventLevel, ConsoleColor?> _map = new Dictionary<EventLevel, ConsoleColor?>()
            {
                { EventLevel.Critical, ConsoleColor.Red },
                { EventLevel.Error, ConsoleColor.Magenta },
                { EventLevel.Informational, ConsoleColor.Green },
                { EventLevel.Verbose, ConsoleColor.Gray },
                { EventLevel.Warning, ConsoleColor.Yellow }
            };

            public ConsoleColor? Map(EventLevel eventLevel)
            {
                ConsoleColor? mapped;
                if (!_map.TryGetValue(eventLevel, out mapped))
                {
                    mapped = null;
                }
                return mapped;
            }
        }

        internal class Formatter : IEventTextFormatter
        {
            private static Dictionary<EventLevel, string> _map = new Dictionary<EventLevel, string>()
            {
                { EventLevel.Critical, "fatal" },
                { EventLevel.Error, "error" },
                { EventLevel.Informational, "info" },
                { EventLevel.Verbose, "trace" },
                { EventLevel.Warning, "warn" }
            };

            private static readonly int MaxLevelLen = Math.Max(3, _map.Values.Max(s => s.Length));

            public void WriteEvent(EventEntry eventEntry, TextWriter writer)
            {
                string levelStr;
                if (!_map.TryGetValue(eventEntry.Schema.Level, out levelStr))
                {
                    levelStr = "<?>";
                }
                levelStr = levelStr.PadRight(MaxLevelLen);
                writer.WriteLine("{0}: {1}", levelStr, eventEntry.FormattedMessage);
            }
        }
    }
}
