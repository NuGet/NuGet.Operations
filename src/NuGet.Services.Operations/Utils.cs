using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations
{
    public static class Utils
    {
        public static string GeneratePassword(bool timestamped)
        {
            string randomness =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        Guid.NewGuid().ToString("N"))).Replace("=", "");

            if (timestamped)
            {
                return DateTime.Now.ToString("MMMddyy") + "!" + randomness;
            }
            else
            {
                return randomness;
            }
        }
    }
}
