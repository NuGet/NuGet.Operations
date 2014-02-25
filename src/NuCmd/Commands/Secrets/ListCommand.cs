using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NuGet.Services.Operations;
using PowerArgs;

namespace NuCmd.Commands.Secrets
{
    [Description("Retrieves a list of available secrets in the store")]
    public class ListCommand : SecretStoreCommandBase
    {
        [ArgShortcut("a")]
        [ArgDescription("Set this switch to include deleted secrets in the list")]
        public bool IncludeDeleted { get; set; }

        protected override async Task OnExecute()
        {
            // Open the store
            var store = await OpenSecretStore();

            // Read the secret
            await Console.WriteInfoLine(Strings.Secrets_ListCommand_Secrets);
            await Console.WriteTable(store.List(IncludeDeleted), i => 
                IncludeDeleted ?
                (object)new
                {
                    Name = i.Name.Name,
                    Datacenter = i.Name.Datacenter,
                    Deleted = i.Deleted
                } : (object)new
                {
                    Name = i.Name.Name,
                    Datacenter = i.Name.Datacenter
                });
        }
    }
}
