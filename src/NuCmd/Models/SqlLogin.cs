using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuCmd.Models
{
    public class SqlLogin : SqlPrincipal
    {
        public string name { get; set; }
        public DateTime create_date { get; set; }
        public DateTime modify_date { get; set; }
    }
}
