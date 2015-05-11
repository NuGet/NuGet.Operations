// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace NuGet.Services.Operations.Secrets.DpapiNg
{
    [SuppressUnmanagedCodeSecurity] // since we're not APTCA
    internal static class NativeMethods
    {
        private const string KERNEL32LIB = "kernel32.dll";
        private const string NCRYPTLIB = "ncrypt.dll";

        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa366730(v=vs.85).aspx
        [DllImport(KERNEL32LIB, SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern IntPtr LocalFree(
            [In] IntPtr handle);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706799(v=vs.85).aspx
        [DllImport(NCRYPTLIB)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal extern static int NCryptCloseProtectionDescriptor(
            [In] IntPtr hDescriptor);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706800(v=vs.85).aspx
        [DllImport(NCRYPTLIB, CharSet = CharSet.Unicode)]
        internal extern static int NCryptCreateProtectionDescriptor(
            [In] string pwszDescriptorString,
            [In] uint dwFlags,
            [Out] out NCryptProtectionDescriptorHandle phDescriptor);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706802(v=vs.85).aspx
        [DllImport(NCRYPTLIB)]
        internal extern static int NCryptProtectSecret(
            [In] NCryptProtectionDescriptorHandle hDescriptor,
            [In] uint dwFlags,
            [In] byte[] pbData,
            [In] uint cbData,
            [In] IntPtr pMemPara,
            [In] IntPtr hWnd,
            [Out] out LocalAllocHandle ppbProtectedBlob,
            [Out] out uint pcbProtectedBlob);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706811(v=vs.85).aspx
        [DllImport(NCRYPTLIB)]
        internal extern static int NCryptUnprotectSecret(
            [In] IntPtr phDescriptor,
            [In] uint dwFlags,
            [In] byte[] pbProtectedBlob,
            [In] uint cbProtectedBlob,
            [In] IntPtr pMemPara,
            [In] IntPtr hWnd,
            [Out] out LocalAllocHandle ppbData,
            [Out] out uint pcbData);
    }
}
