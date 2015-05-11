// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public class Service : NamedModelComponentBase
    {
        public Uri Uri { get; set; }
        public Datacenter Datacenter { get; private set; }

        public string FullName { get { return Datacenter.FullName + "-" + Name; } }

        public Service(Datacenter dc)
        {
            Datacenter = dc;
        }
    }
}
