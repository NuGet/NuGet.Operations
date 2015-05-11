// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services;
using NuGet.Services.Work.Client;
using PowerArgs;

namespace NuCmd.Commands.Work
{
    public abstract class WorkServiceCommandBase : ServiceCommandBase<WorkClient>
    {
        protected WorkServiceCommandBase() : base("work", c => new WorkClient(c)) { }
    }
}
