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

        public DpapiSecretStore(string storeDirectory, SecretStoreMetadata metadata)
            : base(metadata)
        {
            StoreDirectory = storeDirectory;

            _protectionDescriptor = DpapiSecretStoreProvider.GetProtectionDescriptorString(metadata.AllowedUsers);
        }

        public override IEnumerable<SecretListItem> List(bool includeDeleted)
        {
            var files = Directory.EnumerateFiles(StoreDirectory, "*.pjson").Select(s => Tuple.Create(s, false));
            if (includeDeleted)
            {
                files = Enumerable.Concat(
                    files,
                    Directory.EnumerateFiles(StoreDirectory, "*.del").Select(s => Tuple.Create(s, true)));
            }

            return files
                .Select(t => Tuple.Create(Path.GetFileNameWithoutExtension(t.Item1), t.Item2))
                .Where(t => !String.Equals(t.Item1, "metadata.v1", StringComparison.OrdinalIgnoreCase))
                .Select(t => new SecretListItem() {
                    Name = SecretName.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(t.Item1))),
                    Deleted = t.Item2
                });
        }

        public override async Task<bool> Delete(SecretName name, string clientOperation)
        {
            // Write an audit record
            var fileName = GetFileName(name);
            var existingSecret = await UnauditedReadSecret(name, fileName);
            if (existingSecret == null)
            {
                return false;
            }
            existingSecret.AddAuditEntry(await SecretAuditEntry.CreateForLocalUser(clientOperation, SecretAuditAction.Deleted));
            await UnauditedWriteSecret(existingSecret);

            // Change the file extension
            File.Move(fileName, Path.ChangeExtension(fileName, ".del"));
            return true;
        }

        public override async Task<bool> Undelete(SecretName name, string clientOperation)
        {
            // Locate the deleted file
            var fileName = GetFileName(name);
            var deletedName = Path.ChangeExtension(fileName, ".del");
            var deletedSecret = await UnauditedReadSecret(name, deletedName);
            if (deletedSecret == null)
            {
                return false;
            }

            // Write it back to a normal secret file
            deletedSecret.AddAuditEntry(await SecretAuditEntry.CreateForLocalUser(clientOperation, SecretAuditAction.Restored));
            await UnauditedWriteSecret(deletedSecret);

            // Delete the deleted secret :)
            File.Delete(deletedName);
            return true;
        }

        public override async Task Write(Secret secret, string clientOperation)
        {
            // Try to read the secret
            var existingSecret = await UnauditedReadSecret(secret.Name, GetFileName(secret.Name));
            
            // Try to undelete the secret, in case a deleted form exists
            if (existingSecret == null && await Undelete(secret.Name, clientOperation))
            {
                existingSecret = await UnauditedReadSecret(secret.Name, GetFileName(secret.Name));
            }

            if (existingSecret != null)
            {
                // Copy the new data and add audit records
                existingSecret.AddAuditEntry(await SecretAuditEntry.CreateForLocalUser(clientOperation, SecretAuditAction.Changed, existingSecret.Value));
                existingSecret.Update(secret);

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

        public override async Task<IEnumerable<SecretAuditEntry>> ReadAuditLog(SecretName name)
        {
            // Read the secret
            var fileName = GetFileName(name);
            var secret = await UnauditedReadSecret(name, fileName);
            if (secret == null)
            {
                // Try to read the deleted log
                secret = await UnauditedReadSecret(name, Path.ChangeExtension(fileName, ".del"));
            }

            // Still null?
            if (secret == null)
            {
                return Enumerable.Empty<SecretAuditEntry>();
            }

            // No need to write to the audit log this time. Just return the log in a new collection.
            return new List<SecretAuditEntry>(secret.AuditLog);
        }

        public override async Task<Secret> Read(SecretName name, string clientOperation)
        {
            // Read the secret
            var secret = await UnauditedReadSecret(name, GetFileName(name));
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

        private async Task<Secret> UnauditedReadSecret(SecretName name, string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            // Read the file
            var protector = CreateProtector(name);
            return JsonFormat.Deserialize<Secret>(await DpapiSecretStoreProvider.ReadSecretFile(fileName, protector));
        }

        private Task UnauditedWriteSecret(Secret secret)
        {
            // Generate the name of the file
            string secretFile = GetFileName(secret.Name);

            // Write the file
            var protector = CreateProtector(secret.Name);
            return DpapiSecretStoreProvider.WriteSecretFile(secretFile, JsonFormat.Serialize(secret), protector);
        }

        private string GetFileName(SecretName name)
        {
            string fileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(name.ToString())) + ".pjson";
            string secretFile = Path.Combine(StoreDirectory, fileName);
            return secretFile;
        }

        private DataProtector CreateProtector(SecretName name)
        {
            return new DpapiNGDataProtector(
                _protectionDescriptor,
                DpapiSecretStoreProvider.ApplicationName,
                SecretStorePurpose,
                new[] { name.ToString() });
        }
    }
}
