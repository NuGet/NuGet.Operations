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
