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
        public const string Http = Prefix + "Http";
        public const string ServiceLifetime = Prefix + "ServiceLifetime";
        public const string SystemDiagnostics = Prefix + "SystemDiagnostics";

        public const string SystemDiagnosticsProviderId = "{6CC8C9C3-1B86-44DB-A651-C020C3073720}";
    }
}
