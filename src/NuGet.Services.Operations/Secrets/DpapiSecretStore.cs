using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Client;
using NuGet.Services.Operations.Secrets.DpapiNg;

namespace NuGet.Services.Operations.Secrets
{
    public class DpapiSecretStore : SecretStore
    {
        private const string SecretStorePurpose = "Secret";

        private string _protectionDescriptor;

        public string StoreDirectory { get; private set; }
        
        public DpapiSecretStore(string storeDirectory, SecretStoreMetadata metadata) : base(metadata)
        {
            StoreDirectory = storeDirectory;

            _protectionDescriptor = DpapiSecretStoreProvider.GetProtectionDescriptorString(metadata.AllowedUsers);
        }

        public override IEnumerable<string> List()
        {
            return Directory.EnumerateFiles(StoreDirectory, "*.pjson")
                .Select(s => Path.GetFileNameWithoutExtension(s))
                .Where(s => !String.Equals(s, "metadata.v1", StringComparison.OrdinalIgnoreCase))
                .Select(s => Encoding.UTF8.GetString(Convert.FromBase64String(s)));
        }

        public override async Task Write(Secret secret, string clientOperation)
        {
            // Try to read the secret
            var existingSecret = await UnauditedReadSecret(secret.Key);
            if (existingSecret != null)
            {
                // Copy the new data and add audit records
                existingSecret.Update(secret);
                existingSecret.AddAuditEntry(await SecretAuditEntry.CreateForLocalUser(clientOperation, SecretAuditAction.Changed));

                // Now resave the existing secret instead
                secret = existingSecret;
            }
            else
            {
                // Add an audit record
                secret.AddAuditEntry(await SecretAuditEntry.CreateForLocalUser(clientOperation, SecretAuditAction.Created));
            }

            // Write the secret
            await UnauditedWriteSecret(secret);
        }

        public override async Task<IEnumerable<SecretAuditEntry>> ReadAuditLog(string key)
        {
            // Read the secret
            var secret = await UnauditedReadSecret(key);
            if (secret == null)
            {
                return Enumerable.Empty<SecretAuditEntry>();
            }

            // No need to write to the audit log this time. Just return the log in a new collection.
            return new List<SecretAuditEntry>(secret.AuditLog);
        }

        public override async Task<Secret> Read(string key, string clientOperation)
        {
            // Read the secret
            var secret = await UnauditedReadSecret(key);
            if (secret == null)
            {
                return null;
            }

            // Add audit log entry and rewrite
            secret.AddAuditEntry(await SecretAuditEntry.CreateForLocalUser(clientOperation, SecretAuditAction.Retrieved));
            await UnauditedWriteSecret(secret);

            // Return the secret value
            return secret;
        }

        private async Task<Secret> UnauditedReadSecret(string key)
        {
            // Get the name of the file
            var secretFile = GetFileName(key);

            if (!File.Exists(secretFile))
            {
                return null;
            }

            // Read the file
            var protector = CreateProtector(key);
            return JsonFormat.Deserialize<Secret>(await DpapiSecretStoreProvider.ReadSecretFile(secretFile, protector));
        }

        private Task UnauditedWriteSecret(Secret secret)
        {
            // Generate the name of the file
            string secretFile = GetFileName(secret.Key);
            
            // Write the file
            var protector = CreateProtector(secret.Key);
            return DpapiSecretStoreProvider.WriteSecretFile(secretFile, JsonFormat.Serialize(secret), protector);
        }

        private string GetFileName(string key)
        {
            string fileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(key)) + ".pjson";
            string secretFile = Path.Combine(StoreDirectory, fileName);
            return secretFile;
        }

        private DataProtector CreateProtector(string key)
        {
            return new DpapiNGDataProtector(
                _protectionDescriptor,
                DpapiSecretStoreProvider.ApplicationName,
                SecretStorePurpose,
                new[] { key });
        }
    }
}
