using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ApiProto
{
    public interface ISearch
    {
        Task<IList<SearchResult>> Query(string term);
    }
}