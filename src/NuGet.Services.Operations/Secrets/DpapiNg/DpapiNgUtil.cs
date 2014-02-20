using System;
using System.Runtime.InteropServices;

namespace NuGet.Services.Operations.Secrets.DpapiNg
{
    // Protects or unprotects data using DPAPI:NG
    internal static class DpapiNgUtil
    {
        private const uint NCRYPT_SILENT_FLAG = 0x00000040;

        public static byte[] Protect(NCryptProtectionDescriptorHandle descriptor, byte[] data)
        {
            LocalAllocHandle protectedBlobHandle;
            uint cbProtectedBlob;
            int status = NativeMethods.NCryptProtectSecret(descriptor, NCRYPT_SILENT_FLAG, data, (uint)data.Length, IntPtr.Zero, IntPtr.Zero, out protectedBlobHandle, out cbProtectedBlob);
            Util.ThrowExceptionForCryptoStatus(status);

            using (protectedBlobHandle)
            {
                byte[] retVal = new byte[cbProtectedBlob];
                Marshal.Copy(protectedBlobHandle.DangerousGetHandle(), retVal, 0, retVal.Length);
                return retVal;
            }
        }

        public static byte[] Unprotect(byte[] protectedData)
        {
            LocalAllocHandle dataHandle;
            uint cbData;
            int status = NativeMethods.NCryptUnprotectSecret(IntPtr.Zero, NCRYPT_SILENT_FLAG, protectedData, (uint)protectedData.Length, IntPtr.Zero, IntPtr.Zero, out dataHandle, out cbData);
            Util.ThrowExceptionForCryptoStatus(status);

            using (dataHandle)
            {
                byte[] retVal = new byte[cbData];
                Marshal.Copy(dataHandle.DangerousGetHandle(), retVal, 0, retVal.Length);
                return retVal;
            }
        }
    }
}
