using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NuCmd.Commands.Secrets;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Certs
{
    [Description("Stores a certificate in the secret store")]
    public class StoreCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The key to store the certificate under")]
        public string Key { get; set; }

        [ArgRequired]
        [ArgPosition(1)]
        [ArgExistingFile]
        [ArgShortcut("f")]
        [ArgDescription("The certificate file to store. Must be a PFX file with a private key")]
        public string File { get; set; }

        protected override async Task OnExecute()
        {
            // Open the store
            var store = await OpenSecretStore();

            // Load the cert file
            var cert = new X509Certificate2(File, String.Empty, X509KeyStorageFlags.Exportable);
            if (!cert.HasPrivateKey)
            {
                await Console.WriteErrorLine(Strings.Secrets_StoreCertCommand_CertificateHasNoPrivateKey);
                return;
            }

            // Save to a string
            string data = Convert.ToBase64String(cert.Export(X509ContentType.Pkcs12, String.Empty));

            // Determine expiry
            var expiresAt = cert.NotAfter;

            // Save the certificate secret
            string certKey = "cert:" + cert.Thumbprint;
            await Console.WriteInfoLine(Strings.Secrets_StoreCertCommand_SavingCertificate, certKey, expiresAt);
            if (!WhatIf)
            {
                var secret = new Secret(new SecretName(certKey, Datacenter), data, DateTime.UtcNow, expiresAt, SecretType.Certificate);
                await store.Write(secret, "nucmd storecert");
            }

            await Console.WriteInfoLine(Strings.Secrets_StoreCertCommand_SavingCertificateReference, Key, expiresAt);
            if (!WhatIf)
            {
                var secret = new Secret(new SecretName(Key, Datacenter), certKey, DateTime.UtcNow, expiresAt, SecretType.Link);
                await store.Write(secret, "nucmd storecert");
            }
        }
    }
}
