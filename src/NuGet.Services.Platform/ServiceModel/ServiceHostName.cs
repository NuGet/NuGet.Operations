using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuGet.Services.ServiceModel
{
    public struct ServiceHostName : IEquatable<ServiceHostName>
    {
        private static readonly Regex Parser = new Regex(@"^-(?<host>[^\-]+)(?<rest>.+)?$", RegexOptions.IgnoreCase);

        public static readonly ServiceHostName Empty = new ServiceHostName();

        public DatacenterName Datacenter { get; private set; }
        public string Name { get; private set; }

        public ServiceHostName(DatacenterName datacenter, string name)
            : this()
        {
            Guard.NotNullOrEmpty(name, "name");

            Datacenter = datacenter;
            Name = name.ToLowerInvariant();
        }

        public override bool Equals(object obj)
        {
            return obj is ServiceHostName && Equals((ServiceHostName)obj);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Datacenter)
                .Add(Name)
                .CombinedHash;
        }

        public bool Equals(ServiceHostName other)
        {
            return Equals(Datacenter, other.Datacenter) &&
                String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return Datacenter.ToString() + "-" + Name.ToLowerInvariant();
        }

        public static ServiceHostName Parse(string input)
        {
            ServiceHostName result;
            if (!TryParse(input, out result))
            {
                throw new FormatException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.ServiceHostName_InvalidName,
                    input));
            }
            return result;
        }

        public static bool TryParse(string input, out ServiceHostName result)
        {
            string _;
            return TryParseCore(input, out result, out _);
        }

        internal static bool TryParseCore(string input, out ServiceHostName result, out string remainder)
        {
            result = ServiceHostName.Empty;
            remainder = null;

            // Parse the environment name portion
            DatacenterName dcName;
            string shPart;
            if (!DatacenterName.TryParseCore(input, out dcName, out shPart) || String.IsNullOrEmpty(shPart))
            {
                return false;
            }

            var match = Parser.Match(shPart);
            if (!match.Success)
            {
                return false;
            }
            else
            {
                result = new ServiceHostName(
                    dcName,
                    match.Groups["host"].Value);
                if (match.Groups["rest"].Success)
                {
                    remainder = match.Groups["rest"].Value;
                }
                return true;
            }
        }
    }
}
