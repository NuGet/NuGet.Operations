// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    internal static class DataProtectorExtensions
    {
        public static byte[] ProtectString(this DataProtector self, string input)
        {
            return self.Protect(
                    Encoding.UTF8.GetBytes(input));
        }

        public static string UnprotectString(this DataProtector self, byte[] input)
        {
            return Encoding.UTF8.GetString(
                self.Unprotect(input));
        }
    }
}
