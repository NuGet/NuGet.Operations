using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuGet.Services.ServiceModel
{
    public struct EnvironmentName : IEquatable<EnvironmentName>
    {
        private static readonly Regex Parser = new Regex(
            @"^(?<product>[^\-]+)-(?<env>[^\-]+)(?<rest>.+)?$",
            RegexOptions.IgnoreCase);

        public static readonly EnvironmentName Empty = new EnvironmentName();

        public string Product { get; private set; }
        public string Name { get; private set; }

        public EnvironmentName(string product, string name)
            : this()
        {
            Guard.NotNullOrEmpty(product, "product");
            Guard.NotNullOrEmpty(name, "name");

            Product = product.ToLowerInvariant();
            Name = name.ToLowerInvariant();
        }

        public override bool Equals(object obj)
        {
            return obj is EnvironmentName && Equals((EnvironmentName)obj);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Product)
                .Add(Name)
                .CombinedHash;
        }

        public bool Equals(EnvironmentName other)
        {
            return String.Equals(Product, other.Product, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return Product.ToLowerInvariant() + "-" + Name.ToLowerInvariant();
        }

        public static EnvironmentName Parse(string input)
        {
            EnvironmentName result;
            if (!TryParse(input, out result))
            {
                throw new FormatException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.EnvironmentName_InvalidName,
                    input));
            }
            return result;
        }

        public static bool TryParse(string input, out EnvironmentName result)
        {
            string _;
            return TryParseCore(input, out result, out _);
        }

        internal static bool TryParseCore(string input, out EnvironmentName result, out string remainder)
        {
            result = EnvironmentName.Empty;
            remainder = null;

            var match = Parser.Match(input);
            if (!match.Success)
            {
                return false;
            }
            else
            {
                result = new EnvironmentName(
                    match.Groups["product"].Value,
                    match.Groups["env"].Value);
                if (match.Groups["rest"].Success)
                {
                    remainder = match.Groups["rest"].Value;
                }
                return true;
            }
        }
    }
}
