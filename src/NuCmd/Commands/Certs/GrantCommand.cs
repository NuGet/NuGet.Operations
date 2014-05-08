using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Models;
using NuCmd.Commands.Secrets;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Certs
{
    [Description("Grants a certificate access to an Azure Subscription")]
    public class GrantCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The key in the secret store containing the certificate to upload")]
        public string Key { get; set; }

        protected override async Task OnExecute()
        {
            // Get the certificate
            var secret = await ReadSecret(Key);

            if (secret == null)
            {
                await Console.WriteErrorLine(Strings.Secrets_NoSuchSecret, Key);
                return;
            }

            if(secret.Type != SecretType.Certificate)
            {
                await Console.WriteErrorLine(Strings.Certs_UploadCommand_SecretIsNotACertificate, Key);
                return;
            }

            var cert = ReadCertificate(secret);

            // Upload the certificate
            using (var mgmt = CloudContext.Clients.CreateManagementClient(await GetAzureCredentials()))
            {
                await Console.WriteInfoLine(Strings.Certs_GrantCommand_Uploading, cert.Thumbprint, mgmt.Credentials.SubscriptionId);
                if (!WhatIf)
                {
                    var data = cert.Export(X509ContentType.Cert);
                    try
                    {
                        await mgmt.ManagementCertificates.CreateAsync(
                            new ManagementCertificateCreateParameters()
                            {
                                Data = data,
                                PublicKey = cert.PublicKey.EncodedKeyValue.RawData,
                                Thumbprint = cert.Thumbprint
                            },
                            CancellationToken.None);
                    }
                    catch (CloudException cex)
                    {
                        if ((int)cex.Response.StatusCode >= 300 || (int)cex.Response.StatusCode < 200)
                        {
                            throw;
                        }
                        // This API literally throws an exception when it succeeds... Seriously...
                    }
                }
                await Console.WriteInfoLine(Strings.Certs_Uploaded);
            }
        }
    }
}
