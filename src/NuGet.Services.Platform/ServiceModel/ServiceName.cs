using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuGet.Services.ServiceModel
{
    public struct ServiceName : IEquatable<ServiceName>
    {
        private static readonly Regex Parser = new Regex(@"^-(?<service>[^\-]+)(?<rest>.+)?$", RegexOptions.IgnoreCase);

        public static readonly ServiceName Empty = new ServiceName();

        public ServiceHostInstanceName Instance { get; private set; }
        public string Name { get; private set; }

        public ServiceName(ServiceHostInstanceName instance, string name) : this()
        {
            Instance = instance;
            Name = name.ToLowerInvariant();
        }

        public override bool Equals(object obj)
        {
            return obj is ServiceName && Equals((ServiceName)obj);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Instance)
                .Add(Name)
                .CombinedHash;
        }

        public bool Equals(ServiceName other)
        {
            return Equals(Instance, other.Instance) &&
                String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return Instance.ToString() + "-" + Name;
        }

        public static ServiceName Parse(string input)
        {
            ServiceName result;
            if (!TryParse(input, out result))
            {
                throw new FormatException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.ServiceName_InvalidName,
                    input));
            }
            return result;
        }

        public static bool TryParse(string input, out ServiceName result)
        {
            string _;
            return TryParseCore(input, out result, out _);
        }

        internal static bool TryParseCore(string input, out ServiceName result, out string remainder)
        {
            result = ServiceName.Empty;
            remainder = null;

            // Parse the environment name portion
            ServiceHostInstanceName shiName;
            string sPart;
            if (!ServiceHostInstanceName.TryParseCore(input, out shiName, out sPart) || String.IsNullOrEmpty(sPart))
            {
                return false;
            }

            var match = Parser.Match(sPart);
            if (!match.Success)
            {
                return false;
            }
            else
            {
                result = new ServiceName(
                    shiName,
                    match.Groups["service"].Value);
                if (match.Groups["rest"].Success)
                {
                    remainder = match.Groups["rest"].Value;
                }
                return true;
            }
        }
    }
}
