using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute.Models;
using NuCmd.Commands.Secrets;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Certs
{
    [Description("Uploads a certificate to an Azure Service")]
    public class UploadCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The key in the secret store containing the certificate to upload")]
        public string Key { get; set; }

        [ArgRequired]
        [ArgPosition(1)]
        [ArgShortcut("svc")]
        [ArgDescription("The service to upload the certificate to")]
        public string TargetService { get; set; }

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
            using (var compute = CloudContext.Clients.CreateComputeManagementClient(await GetAzureCredentials()))
            {
                await Console.WriteInfoLine(Strings.Certs_UploadCommand_UploadingCert, cert.Thumbprint, TargetService);
                if (!WhatIf)
                {
                    await compute.ServiceCertificates.CreateAsync(
                        TargetService,
                        new ServiceCertificateCreateParameters()
                        {
                            CertificateFormat = CertificateFormat.Pfx,
                            Password = String.Empty,
                            Data = Convert.FromBase64String(secret.Value)
                        });
                }
                await Console.WriteInfoLine(Strings.Certs_Uploaded);
            }
        }
    }
}
