using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace NuGet.Services.Operations
{
    public class AzureToken
    {
        public string SubscriptionId { get; set; }
        public AuthenticationResult Token { get; set; }
    }
}
