// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public class AppModel
    {
        public static readonly Version DefaultVersion = new Version(3, 0);

        public string Name { get; private set; }
        public string DistinguishedName { get; set; }
        public Version Version { get; private set; }

        public IList<DeploymentEnvironment> Environments { get; private set; }
        public IList<AzureSubscription> Subscriptions { get; private set; }

        public IList<Resource> Resources { get; private set; }

        public AppModel() : this(String.Empty, DefaultVersion)
        {
        }

        public AppModel(string name, Version version)
        {
            Name = name;
            Version = Version;

            Environments = new List<DeploymentEnvironment>();
            Subscriptions = new List<AzureSubscription>();
            Resources = new List<Resource>();
        }
    }
}
