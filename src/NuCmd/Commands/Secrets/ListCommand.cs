// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using NuGet.Services.Operations;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Secrets
{
    [Description("Retrieves a list of available secrets in the store")]
    public class ListCommand : SecretStoreCommandBase
    {
        [ArgShortcut("a")]
        [ArgDescription("Set this switch to include deleted secrets in the list")]
        public bool IncludeDeleted { get; set; }

        [ArgPosition(0)]
        [ArgShortcut("f")]
        [ArgDescription("A filter to apply to the secret keys")]
        public string Filter { get; set; }

        [ArgShortcut("r")]
        [ArgDescription("Set this switch to interpret -Filter as a Regular Expression instead of a Wildcard pattern")]
        public bool Regex { get; set; }

        protected override async Task OnExecute()
        {
            // Open the store
            var store = await OpenSecretStore();

            // Read the secret
            await Console.WriteInfoLine(Strings.Secrets_ListCommand_Secrets);
            await Console.WriteTable(
                from secret in store.List(IncludeDeleted)
                where (!Datacenter.HasValue || secret.Name.Datacenter == Datacenter.Value) && (String.IsNullOrEmpty(Filter) || ApplyFilter(secret))
                orderby secret.Name.Datacenter descending, secret.Name.Name
                select IncludeDeleted ?
                    (object)new
                    {
                        Name = secret.Name.Name,
                        Datacenter = secret.Name.Datacenter,
                        Deleted = secret.Deleted
                    } : (object)new
                    {
                        Name = secret.Name.Name,
                        Datacenter = secret.Name.Datacenter
                    });
        }

        private bool ApplyFilter(SecretListItem secret)
        {
            var regex = Regex ?
                new Regex(Filter) :

                // Convert Wildcard to Regex
                //  . => \. to escape it
                //  * => .* - 0 or multiple characters
                //  ? => . - Any single character
                //  Always do a prefix match, so anchor the match but suffix it with .*
                new Regex("^" + Filter.Replace(".", @"\.").Replace("*", ".*").Replace("?", ".") + ".*$");

            return regex.IsMatch(secret.Name.Name);
        }
    }
}
