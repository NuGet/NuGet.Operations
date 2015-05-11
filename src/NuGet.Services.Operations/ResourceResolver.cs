// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations.Model;
using NuGet.Services.Operations.Secrets;

namespace NuGet.Services.Operations
{
    public static class ResourceResolver
    {
        public static readonly Dictionary<string, Func<ResourceResolutionContext, object>> Resolvers = new Dictionary<string, Func<ResourceResolutionContext, object>>(StringComparer.OrdinalIgnoreCase)
        {
            {"azureStorage", ResolveAzureStorage},
            {"sqldb", ResolveSqlDb},
            {"uri", ResolveUri},
            {"oauth", ResolveOAuth}
        };

        public static object Resolve(SecretStore secrets, Service service, Resource resource)
        {
            Func<ResourceResolutionContext, object> resolver;
            if (!Resolvers.TryGetValue(resource.Type, out resolver))
            {
                return String.Empty;
            }
            return resolver(new ResourceResolutionContext(secrets, service, resource)) ?? String.Empty;
        }

        private static object ResolveOAuth(ResourceResolutionContext context)
        {
            return new Dictionary<string, object>() {
                {"id", context.Resource.Value},
                {"secret", context.GetSecretValueOrDefault("oauth." + context.Resource.Name + ":secret")}
            };
        }

        private static object ResolveUri(ResourceResolutionContext context)
        {
            Func<string> userNameThunk = () => context.GetSecretValueOrDefault("uri." + context.Resource.Name + ":username");
            Func<string> passwordThunk = () => context.GetSecretValueOrDefault("uri." + context.Resource.Name + ":password");
            return new Dictionary<string, object>() 
            {
                {"value", context.Resource.Value},
                {"username", new DeferredString(userNameThunk)},
                {"password", new DeferredString(passwordThunk)},
                {"absoluteUri", new DeferredString(() => new UriBuilder(context.Resource.Value) {
                    UserName = WebUtility.UrlEncode(userNameThunk()),
                    Password = passwordThunk()
                }.Uri.AbsoluteUri)}
            };
        }

        private static object ResolveAzureStorage(ResourceResolutionContext context)
        {
            return new DeferredString(() =>
                // Get the connection string, it's in the secret store
                context.GetSecretValueOrDefault("azureStorage." + context.Resource.Value));
        }

        private static object ResolveSqlDb(ResourceResolutionContext context)
        {
            return new Func<string, string>(user => {
                string serverName = Utils.GetServerName(context.Resource);

                if (String.Equals(user, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    // Resolve the admin account
                    var cstr = new SqlConnectionStringBuilder(context.Resource.Value);
                    cstr.UserID = Utils.GetAdminUserName(context.Resource, context.Service.Datacenter);
                    cstr.Password = context.GetSecretValueOrDefault("sqldb." + serverName + ":admin");
                    return cstr.ConnectionString;
                }
                else
                {
                    // Look up the current user name
                    var login = context.GetSecretValueOrDefault("sqldb." + serverName + ":serviceUsers." + context.Service.Name);

                    // Look up the password for that user
                    var password = context.GetSecretValueOrDefault("sqldb." + serverName + ":logins." + login);

                    // Generate a connection string
                    var builder = new SqlConnectionStringBuilder(context.Resource.Value);
                    builder.UserID = login;
                    builder.Password = password;
                    return builder.ConnectionString;
                }
            });
        }

        public class ResourceResolutionContext
        {
            public SecretStore Secrets { get; private set; }
            public Service Service { get; private set; }
            public Resource Resource { get; private set; }

            public ResourceResolutionContext(SecretStore secrets, Service service, Resource resource)
            {
                Secrets = secrets;
                Service = service;
                Resource = resource;
            }

            public string GetSecretValueOrDefault(string name)
            {
                var secret = Secrets.Read(new SecretName(name), "resolve:" + Resource.Type + "." + Resource.Name).Result;
                if (secret == null)
                {
                    return null;
                }
                return secret.Value;
            }
        }

        private class DeferredString
        {
            private Func<string> _deferral;

            public DeferredString(Func<Task<string>> deferral) 
                : this(() => deferral().Result)
            {

            }

            public DeferredString(Func<string> deferral)
            {
                _deferral = deferral;
            }

            public override string ToString()
            {
                return _deferral();
            }

            public static implicit operator string(DeferredString self)
            {
                return self._deferral();
            }
        }
    }
}
