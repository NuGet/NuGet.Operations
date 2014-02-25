using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Secrets
{
    public class SecretName
    {
        public int? Datacenter { get; private set; }
        public string Name { get; private set; }

        public SecretName(string name, int? datacenter)
        {
            Name = name;
            Datacenter = datacenter;
        }

        public override string ToString()
        {
            return 
                (Datacenter.HasValue ? Datacenter.Value.ToString() : "_") +
                ":" +
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Name));
        }

        public static bool TryParse(string input, out SecretName parsed)
        {
            parsed = null;
            string[] segments = input.Split(':');
            if (segments.Length != 2)
            {
                return false;
            }
            string name = Encoding.UTF8.GetString(Convert.FromBase64String(segments[1]));
            if (String.Equals(segments[0], "_", StringComparison.OrdinalIgnoreCase))
            {
                parsed = new SecretName(name, datacenter: null);
            }
            else
            {
                int datacenter;
                if (!Int32.TryParse(segments[0], out datacenter))
                {
                    return false;
                }
                parsed = new SecretName(name, datacenter);
            }
            return true;
        }

        public static SecretName Parse(string input)
        {
            SecretName ret;
            if (!TryParse(input, out ret))
            {
                throw new FormatException(Strings.SecretKey_Invalid);
            }
            return ret;
        }
    }
}
