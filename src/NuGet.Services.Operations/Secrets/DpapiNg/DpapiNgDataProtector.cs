// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;

namespace NuGet.Services.Operations.Secrets.DpapiNg
{
    // Borrowed from http://blogs.msdn.com/b/webdev/archive/2012/10/23/cryptographic-improvements-in-asp-net-4-5-pt-2.aspx
    // Adapted to support arbitrary protection descriptors.

    // This type demonstrates protecting data using DPAPI:NG.
    // See http://msdn.microsoft.com/en-us/library/windows/desktop/hh706794(v=vs.85).aspx for more info.
    // If the process is running as a domain identity and Active Directory is running Windows Server 2012,
    // then the keys will be stored in Active Directory. This makes deploying to a web farm environment
    // much easier.
    public sealed class DpapiNGDataProtector : DataProtector
    {
        private const uint NCRYPT_SILENT_FLAG = 0x00000040;
        private Lazy<NCryptProtectionDescriptorHandle> _protectionDescriptorLazy;

        // ASP.NET will call this API for us automatically. The 'applicationName' parameter comes from config,
        // and the 'primaryPurpose' and 'specificPurposes' parameters are generated automatically.
        public DpapiNGDataProtector(string protectionDescriptor, string applicationName, string primaryPurpose, string[] specificPurposes)
            : base(applicationName, primaryPurpose, specificPurposes)
        {
            _protectionDescriptorLazy = new Lazy<NCryptProtectionDescriptorHandle>(() => NCryptProtectionDescriptorHandle.Create(protectionDescriptor));
        }

        public DpapiNGDataProtector(string applicationName, string primaryPurpose, string[] specificPurposes)
            : base(applicationName, primaryPurpose, specificPurposes)
        {
            _protectionDescriptorLazy = null;
        }


        public override bool IsReprotectRequired(byte[] encryptedData)
        {
            return false;
        }

        protected override byte[] ProviderProtect(byte[] userData)
        {
            if (_protectionDescriptorLazy == null)
            {
                throw new InvalidOperationException(Strings.DpapiNGDataProtector_ProtectorCanOnlyUnprotect);
            }

            // When DataProtector.Protect is called, the base class will automatically
            // combine the purpose strings provided in the ctor with the plaintext data
            // to generate the 'userData' parameter, which is passed in here. This method
            // therefore doesn't need to know about the purpose strings.
            // This behavior can be overridden: see http://msdn.microsoft.com/en-us/library/system.security.cryptography.dataprotector.prependhashedpurposetoplaintext.
            LocalAllocHandle protectedBlobHandle;
            uint cbProtectedBlob;
            int status = NativeMethods.NCryptProtectSecret(_protectionDescriptorLazy.Value, NCRYPT_SILENT_FLAG, userData, (uint)userData.Length, IntPtr.Zero, IntPtr.Zero, out protectedBlobHandle, out cbProtectedBlob);
            Util.ThrowExceptionForCryptoStatus(status);

            using (protectedBlobHandle)
            {
                byte[] retVal = new byte[cbProtectedBlob];
                Marshal.Copy(protectedBlobHandle.DangerousGetHandle(), retVal, 0, retVal.Length);
                return retVal;
            }
        }

        protected override byte[] ProviderUnprotect(byte[] encryptedData)
        {
            // Recall that since the Protect method combines the purpose strings in
            // 'userData', when the encrypted data is deciphered the resulting value
            // will still have the purpose strings combined in the payload. The
            // Provider.Unprotect method will split these apart on our behalf, performing
            // a comparison between the strings present in the payload and the strings
            // passed as ctor parameters. If there is a mismatch, the Unprotect
            // method will throw an exception. The ProviderUnprotect method therefore
            // doesn't need to know about the purpose strings.
            // This behavior can be overridden: see http://msdn.microsoft.com/en-us/library/system.security.cryptography.dataprotector.prependhashedpurposetoplaintext.
            LocalAllocHandle dataHandle;
            uint cbData;
            int status = NativeMethods.NCryptUnprotectSecret(IntPtr.Zero, NCRYPT_SILENT_FLAG, encryptedData, (uint)encryptedData.Length, IntPtr.Zero, IntPtr.Zero, out dataHandle, out cbData);
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
