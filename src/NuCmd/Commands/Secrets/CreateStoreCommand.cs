using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations.Secrets;
using PowerArgs;

namespace NuCmd.Commands.Secrets
{
    [Description("Creates a new secret store for the datacenter.")]
    public class CreateStoreCommand : SecretStoreCommandBase
    {
        [ArgRequired]
        [ArgShortcut("u")]
        [ArgDescription("A list of Active Directory users allowed to access the secret store")]
        public string[] AllowedUsers { get; set; }

        protected override async Task OnExecute()
        {
            var provider = CreateProvider();
            if (!WhatIf)
            {
                await provider.Create(Name, AllowedUsers);
            }
            await Console.WriteInfoLine(Strings.Secret_CreateStoreCommand_Created, Name);
        }
    }
}
