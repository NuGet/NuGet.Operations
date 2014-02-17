using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands.Secret
{
    [Description("[NOT WORKING YET] Gets a secret from the secret store")]
    public class GetCommand : DatacenterCommandBase
    {
        [ArgRequired]
        [ArgShortcut("t")]
        [ArgDescription("The type of resource to get the secret value for")]
        public string ResourceType { get; set; }

        [ArgRequired]
        [ArgShortcut("r")]
        [ArgDescription("The name of the resource to get the secret value for")]
        public string ResourceName { get; set; }

        [ArgShortcut("k")]
        [ArgDescription("The key defining the specific secret value to get, if multiple values are available for this resource")]
        public string Key { get; set; }

        protected override async Task OnExecute()
        {
            throw new NotImplementedException();

            //var dc = GetDatacenter();
            //var conn = dc.Environment.ConnectToSecretStore();

            //var secret = await conn.Get(dc.FullName, ResourceType, ResourceName, Key);
            //await Console.WriteObject(secret);
        }
    }
}
