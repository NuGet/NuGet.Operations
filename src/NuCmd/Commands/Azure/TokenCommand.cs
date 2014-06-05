﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuCmd.Commands.Azure
{
    [Description("Gets your azure login token, if any")]
    public class TokenCommand : EnvironmentCommandBase
    {
        protected override async Task OnExecute()
        {
            if (Session == null ||
                Session.CurrentEnvironment == null ||
                Session.CurrentEnvironment.Subscription == null)
            {
                throw new InvalidOperationException(Strings.AzureCommandBase_RequiresSubscription);
            }

            var token = await Session.AzureTokens.GetToken(Session.CurrentEnvironment.App.AdTenantId);
            if (token == null)
            {
                await Console.WriteInfoLine(Strings.Azure_TokenCommand_NoToken);
            }
            else
            {
                await Console.WriteInfoLine(Strings.Azure_TokenCommand_Token, token.AccessToken);
            }
        }
    }
}
