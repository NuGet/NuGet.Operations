using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using NuGet.Services.Operations.Azure;

namespace NuGet.Services.Operations
{
    public class AzureTokenManager
    {
        private const string AdalAuthority = "https://login.windows.net/{0}";
        private const string ClientId = "074bc578-c33d-4a8a-aeb0-60f6ec6c3c2c";
        private const string AdalResource = "https://management.core.windows.net/";
        private static readonly Uri CallBackUrl = new Uri("http://operations.nuget.org");

        private string _root;

        public AzureTokenManager(string root)
        {
            _root = root;
        }

        public async Task<AuthenticationResult> GetToken(string tenantId)
        {
            // Authenticate!
            var context = CreateContext(tenantId);

            AuthenticationResult result = null;
            await STAHelper.InSTAThread(() =>
                result = context.AcquireToken(
                    AdalResource,
                    ClientId,
                    CallBackUrl,
                    PromptBehavior.Auto));

            return result;
        }

        public async Task<AuthenticationResult> RefreshToken(AuthenticationResult result)
        {
            var context = CreateContext(result.TenantId);

            // Reauthenticate with the refresh token
            if (!String.IsNullOrEmpty(result.RefreshToken))
            {
                try
                {
                    return await context.AcquireTokenByRefreshTokenAsync(
                        result.RefreshToken, ClientId);
                }
                catch (Exception)
                {
                    // Failed to refresh...
                }
            }

            // If we got here, we don't have a refresh token or we failed to refresh with it. Just get a fresh new token
            return await GetToken(result.TenantId);
        }

        private AuthenticationContext CreateContext(string tenantId)
        {
            return new AuthenticationContext(
                String.Format(AdalAuthority, tenantId),
                new CredManCache());
        }
    }
}
