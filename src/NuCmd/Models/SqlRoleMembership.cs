using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuCmd.Models
{
    public class SqlRoleMembership : SqlPrincipal
    {
        public string member { get; set; }
        public string role { get; set; }
    }
}
