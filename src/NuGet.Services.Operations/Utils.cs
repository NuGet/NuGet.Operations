// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations.Model;

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

        public static string GetServerName(string dataSource)
        {
            string server = dataSource;
            if (server.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
            {
                server = server.Substring(4);
            }
            if (server.EndsWith(".database.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                // ".database.windows.net" is 21 characters long
                server = server.Substring(0, server.Length - 21);
            }
            return server;
        }

        public static string GetServerName(Resource resource)
        {
            return GetServerName(new SqlConnectionStringBuilder(resource.Value).DataSource);
        }

        public static string GetAdminUserName(Resource server, Datacenter dc)
        {
            string user;
            if (!server.Attributes.TryGetValue("adminUser", out user) || String.IsNullOrEmpty(user))
            {
                user = String.Format(
                    "{0}-admin",
                    dc.FullName);
            }
            return user;
        }
    }
}
