using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Communications
{
    public enum FailFastState
    {
        /// <summary>
        /// Value that indicates that the service state has not been set. Should never be exposed to users!
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Indicates that the service is active and requests are being satisfied within the timeout.
        /// </summary>
        Active,

        /// <summary>
        /// Indicates that requests have failed, but not enough to trip the fail fast mode.
        /// </summary>
        Faulting,

        /// <summary>
        /// Indicates that requests have timed out, but not enough to trip the fail fast mode.
        /// </summary>
        TimingOut,

        /// <summary>
        /// Indicates that the external service is in fail fast mode and all requests are being rejected.
        /// </summary>
        FailFast,

        /// <summary>
        /// Indicates that the external service is being tested to see if communication can be restored.
        /// </summary>
        TryRestore,

        /// <summary>
        /// Indicates that the external service status unknown as no requests have been made yet.
        /// </summary>
        Unknown
    }
}
