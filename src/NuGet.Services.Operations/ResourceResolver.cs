using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations.Model;
using NuGet.Services.Operations.Secrets;

namespace NuGet.Services.Operations
{
    public static class ResourceResolver
    {
        public static readonly Dictionary<string, Func<ResourceResolutionContext, Task<string>>> Resolvers = new Dictionary<string, Func<ResourceResolutionContext, Task<string>>>(StringComparer.OrdinalIgnoreCase)
        {
            {"azureStorage", ResolveAzureStorage},
            {"sqldb", ResolveSqlDb}
        };

        public static async Task<string> Resolve(SecretStore secrets, Service service, Resource resource)
        {
            Func<ResourceResolutionContext, Task<string>> resolver;
            if (!Resolvers.TryGetValue(resource.Type, out resolver))
            {
                return null;
            }
            return await resolver(new ResourceResolutionContext(secrets, service, resource));
        }

        private static async Task<string> ResolveAzureStorage(ResourceResolutionContext context)
        {
            // Get the connection string, it's in the secret store
            string secretName = "azureStorage." + context.Resource.Value;
            var secret = await context.Secrets.Read(new SecretName(secretName), "ResolveAzureStorage");

            if (secret == null)
            {
                return null;
            }
            else
            {
                return secret.Value;
            }
        }

        private static Task<string> ResolveSqlDb(ResourceResolutionContext context)
        {
            return Task.FromResult("");
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
        }
    }
}
