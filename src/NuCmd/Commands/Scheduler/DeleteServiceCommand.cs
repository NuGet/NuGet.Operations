// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Scheduler;
using PowerArgs;

namespace NuCmd.Commands.Scheduler
{
    [Description("Deletes the specified scheduler service")]
    public class DeleteServiceCommand : AzureConnectionCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("cs")]
        public string Name { get; set; }

        protected override async Task OnExecute(SubscriptionCloudCredentials credentials)
        {
            using (var client = CloudContext.Clients.CreateCloudServiceManagementClient(credentials))
            {
                await Console.WriteInfoLine(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Scheduler_CsDeleteCommand_DeletingService,
                    Name));
                if (!WhatIf)
                {
                    await client.CloudServices.DeleteAsync(Name);
                }
                await Console.WriteInfoLine(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Scheduler_CsDeleteCommand_DeletedService,
                    Name));
            }
        }
    }
}
