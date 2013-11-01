using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Owin.Hosting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using NuGet.Services.Monitoring;

namespace NuGet.Services.Gateway.Role
{
    public class WorkerRole : ServiceHostRole<GatewayService>
    {
    }
}
