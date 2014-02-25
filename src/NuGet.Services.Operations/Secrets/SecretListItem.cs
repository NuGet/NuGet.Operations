using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Secrets
{
    public class SecretListItem
    {
        public SecretName Name { get; set; }
        public bool Deleted { get; set; }
    }
}
