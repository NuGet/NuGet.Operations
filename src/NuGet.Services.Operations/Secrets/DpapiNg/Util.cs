// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Security.Cryptography;

namespace NuGet.Services.Operations.Secrets.DpapiNg
{
    internal static class Util
    {
        internal static void ThrowExceptionForCryptoStatus(int status)
        {
            if (status != 0)
            {
                throw new CryptographicException(status);
            }
        }
    }
}
