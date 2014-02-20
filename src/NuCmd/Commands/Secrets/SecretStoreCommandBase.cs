using System;
using System.Collections.Generic;
using System.Linq;
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
                if (env != null)
                {
                    var store = env.SecretStores.FirstOrDefault(s => String.Equals(s.Type, DpapiSecretStoreProvider.AppModelTypeName, StringComparison.OrdinalIgnoreCase));
                    if (store != null)
                    {
                        StoreRoot = store.Value;
                    }
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
                if (Datacenter.HasValue)
                {
                    await Console.WriteInfoLine(Strings.SecretStoreCommandBase_UsingDatacenterStore);
                    var dc = GetDatacenter();
                    Name = SecretStoreProvider.GetDatacenterStoreName(dc.FullName);
                }
                else
                {
                    await Console.WriteInfoLine(Strings.SecretStoreCommandBase_UsingEnvironmentStore);
                    var env = GetEnvironment();
                    Name = SecretStoreProvider.GetEnvironmentStoreName(env.FullName);
                }
            }

            if (String.IsNullOrEmpty(Name))
            {
                await Console.WriteErrorLine(Strings.SecretStoreCommandBase_StoreNameRequired);
                throw new OperationCanceledException();
            }
        }

        protected virtual async Task<SecretStore> OpenSecretStore()
        {
            var provider = CreateProvider();
            return await provider.Open(Name);
        }
    }
}
