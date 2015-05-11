// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Storage;
using NuCmd.Commands.Db;
using NuGet.Services.Operations;
using NuGet.Services.Operations.Model;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands
{
    public abstract class EnvironmentCommandBase : Command
    {
        [ArgShortcut("e")]
        [ArgDescription("The environment to work in (defaults to the current environment)")]
        public string Environment { get; set; }

        protected virtual DeploymentEnvironment GetEnvironment(bool required)
        {
            return GetEnvironment(Environment, required);
        }

        protected virtual DeploymentEnvironment GetEnvironment()
        {
            return GetEnvironment(Environment);
        }

        protected virtual Datacenter GetDatacenter(int datacenter)
        {
            return GetDatacenter(datacenter, required: true);
        }

        protected virtual Datacenter GetDatacenter(int datacenter, bool required)
        {
            var env = GetEnvironment(required);
            if (env == null)
            {
                return null;
            }
            return GetDatacenter(env, datacenter, required);
        }

        protected async Task<SubscriptionCloudCredentials> GetAzureCredentials()
        {
            if (Session == null ||
                Session.CurrentEnvironment == null ||
                Session.CurrentEnvironment.Subscription == null)
            {
                throw new InvalidOperationException(Strings.AzureCommandBase_RequiresSubscription);
            }

            var token = await Session.AzureTokens.LoadToken(Session.CurrentEnvironment.Subscription.Id);
            if (token == null)
            {
                throw new InvalidOperationException(Strings.AzureCommandBase_RequiresToken);
            }

            return new TokenCloudCredentials(
                Session.CurrentEnvironment.Subscription.Id,
                token.Token.AccessToken);
        }

        protected async Task<IDictionary<string, string>> LoadServiceConfig(Datacenter dc, Service service)
        {
            await Console.WriteInfoLine(Strings.AzureCommandBase_FetchingServiceConfig, service.Value);

            // Get creds
            var creds = await GetAzureCredentials();
            var ns = XNamespace.Get("http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration");

            // Connect to the Compute Management Client
            using (var client = CloudContext.Clients.CreateComputeManagementClient(creds))
            {
                // Download config for the deployment
                var result = await client.Deployments.GetBySlotAsync(service.Value, DeploymentSlot.Production);

                var parsed = XDocument.Parse(result.Configuration);
                return parsed.Descendants(ns + "Setting").ToDictionary(
                    x => x.Attribute("name").Value,
                    x => x.Attribute("value").Value,
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        protected async Task<SqlConnectionInfo> GetSqlConnectionInfo(int datacenter, string dbResource, string specifiedAdminUser, string specifiedAdminPassword, bool promptForPassword)
        {
            // Prep the connection string
            var dc = GetDatacenter(datacenter, required: true);

            // Find the server
            var server = dc.FindResource(ResourceTypes.SqlDb, dbResource);
            if (server == null)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DatabaseCommandBase_NoDatabaseInDatacenter,
                    datacenter,
                    ResourceTypes.SqlDb,
                    dbResource));
            }

            specifiedAdminUser = specifiedAdminUser ?? Utils.GetAdminUserName(server, dc);

            var connStr = new SqlConnectionStringBuilder(server.Value);
            if (String.IsNullOrEmpty(connStr.InitialCatalog))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DatabaseCommandBase_ResourceMissingRequiredConnectionStringField,
                    ResourceTypes.SqlDb,
                    server.Name,
                    "InitialCatalog"));
            }
            if (String.IsNullOrEmpty(connStr.DataSource))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DatabaseCommandBase_ResourceMissingRequiredConnectionStringField,
                    ResourceTypes.SqlDb,
                    server.Name,
                    "DataSource"));
            }
            if (!String.IsNullOrEmpty(connStr.UserID))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DatabaseCommandBase_ResourceHasUnexpectedConnectionStringField,
                    ResourceTypes.SqlDb,
                    server.Name,
                    "User ID"));
            }
            if (!String.IsNullOrEmpty(connStr.Password))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DatabaseCommandBase_ResourceHasUnexpectedConnectionStringField,
                    ResourceTypes.SqlDb,
                    server.Name,
                    "Password"));
            }

            if (String.IsNullOrEmpty(specifiedAdminPassword))
            {
                // Try getting it from the secret store
                var secrets = await GetEnvironmentSecretStore(Session.CurrentEnvironment);
                if (secrets != null)
                {
                    var secret = await secrets.Read(new SecretName("sqldb." + Utils.GetServerName(connStr.DataSource) + ":admin"), Definition.FullName);
                    if (secret != null)
                    {
                        await Console.WriteInfoLine(Strings.Db_DatabaseCommandBase_UsingSecretStore);
                        specifiedAdminPassword = secret.Value;
                    }
                }
            }

            SecureString password;
            if (String.IsNullOrEmpty(specifiedAdminPassword))
            {
                if (!promptForPassword)
                {
                    throw new InvalidOperationException(String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Db_DatabaseCommandBase_MissingAdminPassword,
                        server.Name));
                }
                // Prompt the user for the admin password and put it in a SecureString.
                password = await Console.PromptForPassword(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Db_DatabaseCommandBase_EnterAdminPassword,
                    specifiedAdminUser));
            }
            else
            {
                // Stuff the password in a secure string and vainly attempt to clear it from unsecured memory.
                password = new SecureString();
                foreach (var chr in specifiedAdminPassword)
                {
                    password.AppendChar(chr);
                }
                specifiedAdminPassword = null;
                GC.Collect(); // Futile effort to remove AdminPassword from memory
                password.MakeReadOnly();
            }

            // Create a SQL Credential and return the connection info
            return new SqlConnectionInfo(connStr, new SqlCredential(specifiedAdminUser, password));
        }

        protected async Task<AzureDefaults> LoadDefaultsFromAzure(Datacenter dc, string databaseConnectionString = null, string storageConnectionString = null)
        {
            bool expired = false;
            try
            {
                if (String.IsNullOrWhiteSpace(databaseConnectionString) ||
                    String.IsNullOrWhiteSpace(storageConnectionString))
                {
                    var config = await LoadServiceConfig(dc, dc.GetService("work"));

                    databaseConnectionString = databaseConnectionString ??
                        GetValueOrDefault(config, "Sql.Legacy");
                    storageConnectionString = storageConnectionString ??
                        GetValueOrDefault(config, "Storage.Legacy");
                }

                if (String.IsNullOrWhiteSpace(databaseConnectionString) ||
                    String.IsNullOrWhiteSpace(storageConnectionString))
                {
                    throw new InvalidOperationException(Strings.Command_MissingEnvironmentArguments);
                }

                await Console.WriteInfoLine(
                    Strings.Command_ConnectionInfo,
                    new SqlConnectionStringBuilder(databaseConnectionString).DataSource,
                    CloudStorageAccount.Parse(storageConnectionString).Credentials.AccountName);
            }
            catch (CloudException ex)
            {
                if (ex.ErrorCode == "AuthenticationFailed")
                {
                    expired = true;
                }
                else
                {
                    throw;
                }
            }

            if (expired)
            {
                await Console.WriteErrorLine(Strings.AzureCommandBase_TokenExpired);
                throw new OperationCanceledException();
            }

            return new AzureDefaults { DatabaseConnectionString = databaseConnectionString, StorageConnectionString = storageConnectionString };
        }

        private string GetValueOrDefault(IDictionary<string, string> dict, string key)
        {
            string val;
            if (!dict.TryGetValue(key, out val))
            {
                return null;
            }
            return val;
        }

        public struct AzureDefaults
        {
            public string DatabaseConnectionString { get; set; }
            public string StorageConnectionString { get; set; }
        }
    }
}
