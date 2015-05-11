// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations.Model;

namespace NuGet.Services.Operations.Secrets
{
    public abstract class SecretStoreProvider
    {
        public abstract Task<SecretStore> Create(string store, IEnumerable<string> allowedUsers);
        public abstract Task<SecretStore> Open(string store);
        public abstract IEnumerable<string> ListStores();


        public virtual Task<SecretStore> Open(DeploymentEnvironment env)
        {
            return Open(GetEnvironmentStoreName(env.FullName));
        }


        public static string GetEnvironmentStoreName(string environment)
        {
            return "env#" + environment;
        }
    }
}
