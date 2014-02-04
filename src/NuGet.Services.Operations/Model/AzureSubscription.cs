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
    }
}
