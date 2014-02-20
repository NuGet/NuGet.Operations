using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using NuGet.Services.Operations.Secrets.DpapiNg;
using NuGet.Services.Client;

namespace NuGet.Services.Operations.Secrets
{
    public class DpapiSecretStoreProvider : SecretStoreProvider
    {
        private string _rootFolder;

        public static readonly string AppModelTypeName = "dpapi";

        internal const string ApplicationName = "NuOps";
        private const string MetadataPurpose = "Metadata";

        public DpapiSecretStoreProvider(string rootFolder)
        {
            _rootFolder = rootFolder;
        }

        public override IEnumerable<string> ListStores()
        {
            // No datacenters there if there's no root folder
            if (!Directory.Exists(_rootFolder))
            {
                return Enumerable.Empty<string>();
            }

            // List the folders containing metadata files
            return Directory.EnumerateDirectories(_rootFolder)
                .Where(s => File.Exists(Path.Combine(s, "metadata.v1.pjson")))
                .Select(s => Path.GetFileName(s));
        }

        public override async Task<SecretStore> Create(string store, IEnumerable<string> allowedUsers)
        {
            // Create the root folder if it does not exist
            if (!Directory.Exists(_rootFolder))
            {
                Directory.CreateDirectory(_rootFolder);
            }

            // Check if there is already a secret store here
            string storeDirectory = Path.Combine(_rootFolder, store);
            if (Directory.Exists(storeDirectory))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DpapiSecretStoreProvider_StoreExists,
                    store));
            }
            Directory.CreateDirectory(storeDirectory);

            // Create the directory and the metadata file
            string metadataFile = Path.Combine(storeDirectory, "metadata.v1.pjson");
            var metadata = new SecretStoreMetadata()
            {
                AllowedUsers = allowedUsers,
                Datacenter = store
            };

            // Encrypt and Save it!
            var protector = CreateProtector(allowedUsers, MetadataPurpose);
            await WriteSecretFile(metadataFile, JsonFormat.Serialize(metadata), protector);
            
            return new DpapiSecretStore(storeDirectory, metadata);
        }

        public override async Task<SecretStore> Open(string store)
        {
            string storeDirectory = Path.Combine(_rootFolder, store);
            if (!Directory.Exists(storeDirectory))
            {
                throw new DirectoryNotFoundException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DpapiSecretStoreProvider_StoreDoesNotExist,
                    store));
            }

            string metadataFile = Path.Combine(storeDirectory, "metadata.v1.pjson");
            if(!File.Exists(metadataFile)) {
                throw new FileNotFoundException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DpapiSecretStoreProvider_MissingMetadata,
                    metadataFile));
            }

            
            // Load encrypted metadata
            var protector = CreateProtector(MetadataPurpose);
            var decrypted = await ReadSecretFile(metadataFile, protector);
            var metadata = JsonFormat.Deserialize<SecretStoreMetadata>(decrypted);
            
            return new DpapiSecretStore(storeDirectory, metadata);
        }

        private DpapiNGDataProtector CreateProtector(string purpose)
        {
            return new DpapiNGDataProtector(ApplicationName, purpose, new string[0]);
        }

        private DpapiNGDataProtector CreateProtector(IEnumerable<string> allowedUsers, string purpose)
        {
            // Generate a descriptor string
            string descriptor = GetProtectionDescriptorString(allowedUsers);

            return new DpapiNGDataProtector(
                descriptor, ApplicationName, purpose, new string[0]);
        }

        internal static async Task WriteSecretFile(string filePath, string content, DataProtector protector)
        {
            var encrypted = protector.ProtectString(content);
            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.WriteAsync(encrypted, 0, encrypted.Length);
            }
        }

        internal static async Task<string> ReadSecretFile(string filePath, DataProtector protector)
        {
            // Load file content
            byte[] encrypted;
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var ms = new MemoryStream())
                {
                    int read;
                    byte[] buffer = new byte[16 * 1024];
                    while ((read = await file.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    encrypted = ms.ToArray();
                }
            }

            // Decrypt it
            return protector.UnprotectString(encrypted);
        }

        internal static string GetProtectionDescriptorString(IEnumerable<string> allowedUsers)
        {
            string descriptor = String.Join(" OR ", allowedUsers
                .Select(s => "SID=" + (new NTAccount(s)).Translate(typeof(SecurityIdentifier)).ToString()));
            return descriptor;
        }
    }
}
