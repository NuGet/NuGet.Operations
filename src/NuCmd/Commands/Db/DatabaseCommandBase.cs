// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NuGet.Services;
using NuGet.Services.Operations;
using NuGet.Services.Operations.Model;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Db
{
    public abstract class DatabaseCommandBase : DatacenterCommandBase
    {
        [ArgRequired]
        [ArgShortcut("db")]
        [ArgDescription("The type of the SQL Database to create the user on")]
        public KnownSqlConnection Database { get; set; }

        [ArgShortcut("au")]
        [ArgDescription("The Admin User Name for the database. Normally not required if the admin is named using the default pattern.")]
        public string AdminUser { get; set; }

        [ArgShortcut("pass")]
        [ArgDescription("The Admin Password. DO NOT SPECIFY THIS WHEN RUNNING INTERACTIVELY!")]
        public string AdminPassword { get; set; }

        protected Task<SqlConnectionInfo> GetSqlConnectionInfo()
        {
            return GetSqlConnectionInfo(Database.ToString(), AdminUser, AdminPassword, promptForPassword: true);
        }
    }

    public class SqlConnectionInfo
    {
        public SqlConnectionStringBuilder ConnectionString { get; private set; }
        public SqlCredential Credential { get; private set; }

        public SqlConnectionInfo(SqlConnectionStringBuilder connectionString, SqlCredential credential)
        {
            ConnectionString = connectionString;
            Credential = credential;
        }

        public Task<SqlConnection> Connect()
        {
            return ConnectCore(ConnectionString);
        }

        public Task<SqlConnection> Connect(string alternateDatabase)
        {
            var connStr = new SqlConnectionStringBuilder(ConnectionString.ConnectionString)
            {
                InitialCatalog = alternateDatabase
            };
            return ConnectCore(connStr);
        }

        public string GetServerName()
        {
            return Utils.GetServerName(ConnectionString.DataSource);
        }

        private async Task<SqlConnection> ConnectCore(SqlConnectionStringBuilder connStr)
        {
            var conn = Credential == null ?
                new SqlConnection(connStr.ConnectionString) :
                new SqlConnection(connStr.ConnectionString, Credential);

            await conn.OpenAsync();
            return conn;
        }
    }
}
