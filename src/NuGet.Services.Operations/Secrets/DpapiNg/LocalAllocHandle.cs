using System;
using System.Runtime.InteropServices;

namespace NuGet.Services.Operations.Secrets.DpapiNg
{
    // Used to point to memory that was allocated via LocalAlloc.
    internal sealed class LocalAllocHandle : SafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private LocalAllocHandle() : base(IntPtr.Zero, ownsHandle: true) { }

        // Do not provide a finalizer - SafeHandle's critical finalizer will
        // call ReleaseHandle for you.

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            IntPtr retVal = NativeMethods.LocalFree(handle);
            return (retVal == IntPtr.Zero);
        }
    }
}
