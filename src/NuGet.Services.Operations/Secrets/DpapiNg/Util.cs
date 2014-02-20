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
