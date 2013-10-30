using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Monitoring
{
    internal static class EventSourceNames
    {
        public const string Prefix = "NuGet-";
        public const string RequestTrace = Prefix + "HttpRequests";
        public const string NetFxTrace = Prefix + "NetFxTrace";

        public const string NetFxTraceProviderId = "{6CC8C9C3-1B86-44DB-A651-C020C3073720}";
    }
}
