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
        public Dictionary<KnownSqlServer, SqlConnectionStringBuilder> Connections { get; private set; }
        
        public SqlConnectionStringBuilder Primary { get { return GetConnectionString(KnownSqlServer.Primary); } }
        public SqlConnectionStringBuilder Legacy { get { return GetConnectionString(KnownSqlServer.Legacy); } }
        public SqlConnectionStringBuilder Warehouse { get { return GetConnectionString(KnownSqlServer.Warehouse); } }

        public SqlConnectionStringBuilder GetConnectionString(KnownSqlServer account)
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
            Connections = Enum.GetValues(typeof(KnownSqlServer))
                .OfType<KnownSqlServer>()
                .Select(a => new KeyValuePair<KnownSqlServer, string>(a, hub.GetSetting(prefix + a.ToString())))
                .Where(p => !String.IsNullOrEmpty(p.Value))
                .ToDictionary(p => p.Key, p => new SqlConnectionStringBuilder(p.Value));
        }
    }
}
