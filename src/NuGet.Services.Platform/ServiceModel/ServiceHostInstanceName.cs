using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuGet.Services.ServiceModel
{
    public struct ServiceHostInstanceName : IEquatable<ServiceHostInstanceName>
    {
        private static readonly Regex Parser = new Regex(@"^_IN(?<id>[0-9]+)(?<rest>.+)?$", RegexOptions.IgnoreCase);

        public static readonly ServiceHostInstanceName Empty = new ServiceHostInstanceName();

        public ServiceHostName Host { get; private set; }
        public int Id { get; private set; }

        public ServiceHostInstanceName(ServiceHostName host, int id)
            : this()
        {
            Guard.NonNegative(id, "id");

            Host = host;
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return obj is ServiceHostInstanceName && Equals((ServiceHostInstanceName)obj);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Host)
                .Add(Id)
                .CombinedHash;
        }

        public bool Equals(ServiceHostInstanceName other)
        {
            return Equals(Host, other.Host) &&
                Id == other.Id;
        }

        public override string ToString()
        {
            return Host.ToString() + "_IN" + Id.ToString();
        }

        public static ServiceHostInstanceName Parse(string input)
        {
            ServiceHostInstanceName result;
            if (!TryParse(input, out result))
            {
                throw new FormatException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.ServiceHostInstanceName_InvalidName,
                    input));
            }
            return result;
        }

        public static bool TryParse(string input, out ServiceHostInstanceName result)
        {
            string _;
            return TryParseCore(input, out result, out _);
        }

        internal static bool TryParseCore(string input, out ServiceHostInstanceName result, out string remainder)
        {
            result = ServiceHostInstanceName.Empty;
            remainder = null;

            // Parse the environment name portion
            ServiceHostName shName;
            string shiPart;
            if (!ServiceHostName.TryParseCore(input, out shName, out shiPart) || String.IsNullOrEmpty(shiPart))
            {
                return false;
            }

            var match = Parser.Match(shiPart);
            if (!match.Success)
            {
                return false;
            }
            else
            {
                result = new ServiceHostInstanceName(
                    shName,
                    Int32.Parse(match.Groups["id"].Value));
                if (match.Groups["rest"].Success)
                {
                    remainder = match.Groups["rest"].Value;
                }
                return true;
            }
        }
    }
}
