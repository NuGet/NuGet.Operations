using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace NuGet.Services.Configuration
{
    public class SqlConfiguration : ICustomConfigurationSection
    {
        public Dictionary<KnownSqlConnection, SqlConnectionStringBuilder> Connections { get; private set; }
        
        public SqlConnectionStringBuilder Primary { get { return GetConnectionString(KnownSqlConnection.Primary); } }
        public SqlConnectionStringBuilder Legacy { get { return GetConnectionString(KnownSqlConnection.Legacy); } }
        public SqlConnectionStringBuilder Warehouse { get { return GetConnectionString(KnownSqlConnection.Warehouse); } }

        public SqlConnectionStringBuilder GetConnectionString(KnownSqlConnection account)
        {
            SqlConnectionStringBuilder connectionString;
            if (Connections.TryGetValue(account, out connectionString))
            {
                return null;
            }
            return connectionString;
        }

        public void Resolve(string prefix, ConfigurationHub hub)
        {
            Connections = Enum.GetValues(typeof(KnownSqlConnection))
                .OfType<KnownSqlConnection>()
                .Select(a => new KeyValuePair<KnownSqlConnection, string>(a, hub.GetSetting(prefix + a.ToString())))
                .Where(p => !String.IsNullOrEmpty(p.Value))
                .ToDictionary(p => p.Key, p => new SqlConnectionStringBuilder(p.Value));
        }
    }
}
