using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuCmd.Models
{
    public class SqlPrincipal
    {
        public byte[] sid { get; set; }

        public string sid_string
        {
            get { return sid == null ? String.Empty : Convert.ToBase64String(sid); }
        }
    }
}
