using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuCmd.Models
{
    public class SqlUser : SqlPrincipal
    {
        public int uid { get; set; }
        public string name { get; set; }
    }
}
