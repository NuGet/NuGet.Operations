using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NuCmd.Commands.Secrets;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Certs
{
    public class ImportCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The key in the secret store containing the certificate to import")]
        public string Key { get; set; }

        [ArgShortcut("sn")]
        [ArgDescription("The name of the store to import the certificate in to")]
        [DefaultValue(StoreName.My)]
        public StoreName StoreName { get; set; }

        [ArgShortcut("sl")]
        [ArgDescription("The location of the store to import the certificate in to")]
        [DefaultValue(StoreLocation.CurrentUser)]
        public StoreLocation StoreLocation { get; set; }

        [ArgShortcut("pub")]
        [ArgDescription("Only include the public key")]
        public bool PublicOnly { get; set; }

        protected override async Task OnExecute()
        {
            // Get the certificate
            var secret = await ReadSecret(Key);

            if (secret == null)
            {
                await Console.WriteErrorLine(Strings.Secrets_NoSuchSecret, Key);
                return;
            }

            if (secret.Type != SecretType.Certificate)
            {
                await Console.WriteErrorLine(Strings.Certs_UploadCommand_SecretIsNotACertificate, Key);
                return;
            }

            var store = new X509Store(StoreName, StoreLocation);
            store.Open(OpenFlags.ReadWrite);

            // Write to our own temp file because the X509Certificate2 ctor
            // that takes a byte array writes a temp file and then never cleans it up
            string temp = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(temp, Convert.FromBase64String(secret.Value));
                var cert = new X509Certificate2(temp, String.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                if (PublicOnly)
                {
                    // Strip the private key
                    File.WriteAllBytes(temp, cert.Export(X509ContentType.Cert));
                    cert = new X509Certificate2(temp, String.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                }
                
                await Console.WriteInfoLine(Strings.Certs_ImportCommand_Importing, cert.Thumbprint, StoreName, StoreLocation);
                store.Add(cert);
                store.Close();
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
        }
    }
}
