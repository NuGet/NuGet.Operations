// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public class AzureSubscription
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public X509Certificate2 Certificate { get; set; }

        public void ResolveCertificate()
        {
            ResolveCertificate(StoreName.My, StoreLocation.CurrentUser);
        }

        public void ResolveCertificate(StoreName name, StoreLocation location)
        {
            ResolveCertificate(new X509Store(name, location));
        }

        public void ResolveCertificate(X509Store store)
        {
            if (Certificate != null)
            {
                return;
            }

            string certName = "Azure-" + Name.Replace(" ", "");

            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(
                X509FindType.FindBySubjectName, certName, validOnly: false);
            Certificate = certs.OfType<X509Certificate2>().FirstOrDefault();
        }
    }
}
