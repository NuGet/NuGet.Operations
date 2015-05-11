// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Search.Client;
using PowerArgs;

namespace NuCmd.Commands.Search
{
    public abstract class SearchServiceCommandBase : ServiceCommandBase<SearchClient>
    {
        protected SearchServiceCommandBase() : base("search", c => new SearchClient(c)) { }
    }
}
