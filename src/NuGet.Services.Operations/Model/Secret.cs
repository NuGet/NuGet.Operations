using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Operations.Model
{
    public class Secret
    {
        public string ResourceType { get; set; }
        public string ResourceName { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Value { get; set; }
        public IList<string> Scopes { get; private set; }

        public string ModifiedBy { get; set; }
        public DateTime ModifiedAt { get; set;  }

        public Secret()
        {
            Scopes = new List<string>();
        }
    }
}
