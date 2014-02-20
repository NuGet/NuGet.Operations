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
