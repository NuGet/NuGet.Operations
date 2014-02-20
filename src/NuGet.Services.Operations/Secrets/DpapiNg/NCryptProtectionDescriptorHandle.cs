using System;
using System.Runtime.InteropServices;

namespace NuGet.Services.Operations.Secrets.DpapiNg
{
    // Represents a protection descriptor handle by DPAPI:NG
    internal sealed class NCryptProtectionDescriptorHandle : SafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private NCryptProtectionDescriptorHandle() : base(IntPtr.Zero, ownsHandle: true) { }

        // Do not provide a finalizer - SafeHandle's critical finalizer will
        // call ReleaseHandle for you.

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        public static NCryptProtectionDescriptorHandle Create(string protectionDescriptor)
        {
            NCryptProtectionDescriptorHandle descriptorHandle;
            int status = NativeMethods.NCryptCreateProtectionDescriptor(protectionDescriptor, 0, out descriptorHandle);
            Util.ThrowExceptionForCryptoStatus(status);
            return descriptorHandle;
        }

        protected override bool ReleaseHandle()
        {
            int retVal = NativeMethods.NCryptCloseProtectionDescriptor(handle);
            return (retVal == 0);
        }
    }
}
