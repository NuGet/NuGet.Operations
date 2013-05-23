using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;

namespace ApiProto
{
    public class SearchResult
    {
        public string Uri { get; set; }
        public JObject Details { get; set; }
        public string Explanation { get; set; }
    }
}