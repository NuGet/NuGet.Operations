// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
