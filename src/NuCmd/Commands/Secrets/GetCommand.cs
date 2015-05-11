// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NuGet.Services.Operations;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Secrets
{
    [Description("Retrieves a value from the secret store")]
    public class GetCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("k")]
        [ArgDescription("The name of the key to get")]
        public string Key { get; set; }

        [ArgShortcut("clip")]
        [ArgDescription("Set this switch to copy the value into the clipboard instead of printing it to the console")]
        public bool CopyToClipboard { get; set; }

        protected override async Task OnExecute()
        {
            // Read the secret
            var secret = await ReadSecret(Key);

            // Check for null
            if (secret == null)
            {
                await Console.WriteErrorLine(Strings.Secrets_GetCommand_SecretDoesNotExist, Key);
            }
            else
            {
                if (secret.Type == SecretType.Certificate)
                {
                    X509Certificate2 cert = ReadCertificate(secret);

                    if (CopyToClipboard)
                    {
                        await STAHelper.InSTAThread(() => Clipboard.SetText(cert.Thumbprint));
                        await Console.WriteInfoLine(Strings.Secrets_GetCommand_ThumbprintCopied, Key);
                    }
                    else
                    {
                        await Console.WriteInfoLine(
                            Strings.Secrets_GetCommand_CertificateMetadata,
                            cert.Thumbprint,
                            cert.Subject,
                            cert.NotAfter);
                    }
                }
                else
                {
                    if (CopyToClipboard)
                    {
                        await STAHelper.InSTAThread(() => Clipboard.SetText(secret.Value));
                        await Console.WriteInfoLine(Strings.Secrets_GetCommand_SecretCopied, Key);
                    }
                    else
                    {
                        await Console.WriteInfoLine(Strings.Secrets_GetCommand_SecretValue, Key);
                        await Console.WriteDataLine(secret.Value);
                    }
                }
            }
        }
    }
}
