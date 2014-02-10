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

        /// <summary>
        /// This represents the export service endpoint used to export a database into a bacpac file
        /// NOTE that this endpoint is different for different datacenters
        /// </summary>
        public string ExportEndPoint { get; private set; }

        /// <summary>
        /// This represents the import service endpoint used to import a database from a bacpac file
        /// NOTE that this endpoint is different for different datacenters
        /// </summary>
        public string ImportEndPoint { get; private set; }

        public SqlConnectionStringBuilder GetConnectionString(KnownSqlConnection account)
        {
            SqlConnectionStringBuilder connectionString;
            if (!Connections.TryGetValue(account, out connectionString))
            {
                return null;
            }
            return connectionString;
        }

        public void Resolve(string prefix, ConfigurationHub hub)
        {
            Connections = GetConnections(hub, prefix, String.Empty);
            ExportEndPoint = hub.GetSetting(prefix + "ExportEndpoint");
            ImportEndPoint = hub.GetSetting(prefix + "ImportEndpoint");
        }

        private static Dictionary<KnownSqlConnection, SqlConnectionStringBuilder> GetConnections(ConfigurationHub hub, string prefix, string suffix)
        {
            return Enum.GetValues(typeof(KnownSqlConnection))
                .OfType<KnownSqlConnection>()
                .Select(a => new KeyValuePair<KnownSqlConnection, string>(a, hub.GetSetting(prefix + a.ToString() + suffix)))
                .Where(p => !String.IsNullOrEmpty(p.Value))
                .ToDictionary(p => p.Key, p => new SqlConnectionStringBuilder(p.Value));
        }
    }
}
