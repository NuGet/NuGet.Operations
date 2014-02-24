using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace NuGet.Services.Operations
{
    public class AzureTokenManager
    {
        private string _root;

        public AzureTokenManager(string root)
        {
            _root = root;
        }

        public async Task<AzureToken> Authenticate(string subscriptionId)
        {
            // Authenticate!
            var context = new AuthenticationContext("https://login.windows.net/common");
            AuthenticationResult result = null;
            await STAHelper.InSTAThread(() =>
                result = context.AcquireToken(
                    "https://management.core.windows.net/",
                    "1950a258-227b-4e31-a9cf-717495945fc2",
                    new Uri("urn:ietf:wg:oauth:2.0:oob"),
                    PromptBehavior.Auto));
            if (result == null)
            {
                return null;
            }

            return new AzureToken()
            {
                SubscriptionId = subscriptionId,
                Token = result
            };
        }

        public async Task<AzureToken> LoadToken(string subscriptionId)
        {
            string path = Path.Combine(_root, "Subscriptions", subscriptionId + ".dat");
            if (!File.Exists(path))
            {
                return null;
            }

            string content = null;
            using (var reader = new StreamReader(path))
            {
                content = await reader.ReadToEndAsync();
            }
            if (content == null)
            {
                return null;
            }
            var unprotected =
                Encoding.UTF8.GetString(
                    ProtectedData.Unprotect(
                        Convert.FromBase64String(content),
                        null,
                        DataProtectionScope.CurrentUser));
            
            return JsonFormat.Deserialize<AzureToken>(unprotected);
        }

        public async Task StoreToken(AzureToken token)
        {
            string dir = Path.Combine(_root, "Subscriptions");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string path = Path.Combine(dir, token.SubscriptionId + ".dat");

            string content = JsonFormat.Serialize(token);
            var protectedData = Convert.ToBase64String(
                ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(content),
                    null,
                    DataProtectionScope.CurrentUser));

            using (var writer = new StreamWriter(path))
            {
                await writer.WriteAsync(protectedData);
            }
        }
    }
}
