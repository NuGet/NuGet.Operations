using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Secrets
{
    public abstract class SecretStoreProviderCommandBase : DatacenterCommandBase
    {
        [ArgShortcut("s")]
        [ArgDescription("The root folder of the secret store. Defaults to the value in the app model if not specified")]
        public string StoreRoot { get; set; }

        protected override async Task LoadDefaultsFromContext()
        {
            await base.LoadDefaultsFromContext();

            // Check if there is a compatible secret store for this environment
            if (String.IsNullOrEmpty(StoreRoot))
            {
                var env = GetEnvironment();
                if (env != null && env.SecretStore != null)
                {
                    if (!String.Equals(DpapiSecretStoreProvider.AppModelTypeName, env.SecretStore.Type))
                    {
                        await Console.WriteErrorLine(Strings.SecretStoreProviderCommandBase_UnknownType, env.SecretStore.Type);
                        throw new OperationCanceledException();
                    }
                    StoreRoot = env.SecretStore.Value;
                }
            }

            if (String.IsNullOrEmpty(StoreRoot))
            {
                await Console.WriteErrorLine(Strings.SecretStoreProviderCommandBase_StoreRootMustBeProvided);
                throw new OperationCanceledException();
            }
        }

        protected virtual SecretStoreProvider CreateProvider()
        {
            return new DpapiSecretStoreProvider(StoreRoot);
        }
    }

    public abstract class SecretStoreCommandBase : SecretStoreProviderCommandBase
    {
        [ArgShortcut("nm")]
        [ArgDescription("The name of the secret store to get, defaults to the current datacenter's default store")]
        public string Name { get; set; }

        protected override async Task LoadDefaultsFromContext()
        {
            await base.LoadDefaultsFromContext();

            if (String.IsNullOrEmpty(Name))
            {
                await Console.WriteInfoLine(Strings.SecretStoreCommandBase_UsingEnvironmentStore);
                var env = GetEnvironment();
                Name = SecretStoreProvider.GetEnvironmentStoreName(env.FullName);
            }

            if (String.IsNullOrEmpty(Name))
            {
                await Console.WriteErrorLine(Strings.SecretStoreCommandBase_StoreNameRequired);
                throw new OperationCanceledException();
            }

            if (Datacenter.HasValue)
            {
                await Console.WriteInfoLine(Strings.SecretStoreCommandBase_DatacenterScope, Datacenter.Value);
            }
        }

        protected virtual async Task<SecretStore> OpenSecretStore()
        {
            var provider = CreateProvider();
            return await provider.Open(Name);
        }

        protected virtual async Task<Secret> ReadSecret(string key)
        {
            return await ReadSecretAndFollowLinks(key, Datacenter, await OpenSecretStore());
        }
        
        protected virtual async Task<Secret> ReadSecretAndFollowLinks(string key, int? datacenter, SecretStore store)
        {
            var secret = await store.Read(key, datacenter, "nucmd get");

            while (secret != null && secret.Type == SecretType.Link)
            {
                // Follow link
                await Console.WriteInfoLine(Strings.Secrets_FollowingLink, secret.Value);
                secret = await store.Read(
                    new SecretName(secret.Value, Datacenter),
                    String.Format(CultureInfo.InvariantCulture, "nucmd get (link from {0})", secret.Name));
            }
            return secret;
        }

        protected virtual X509Certificate2 ReadCertificate(Secret secret)
        {
            // Write to our own temp file because the X509Certificate2 ctor
            // that takes a byte array writes a temp file and then never cleans it up
            string temp = Path.GetTempFileName();
            X509Certificate2 cert;
            try
            {
                File.WriteAllBytes(temp, Convert.FromBase64String(secret.Value));
                cert = new X509Certificate2(temp, String.Empty);
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
            return cert;
        }
    }
}
