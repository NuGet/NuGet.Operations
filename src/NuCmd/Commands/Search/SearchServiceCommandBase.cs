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
