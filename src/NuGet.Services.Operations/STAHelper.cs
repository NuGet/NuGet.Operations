// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Services.Operations
{
    public static class STAHelper
    {
        public static Task InSTAThread(Action act)
        {
            var tcs = new TaskCompletionSource<object>();
            var t = new Thread(() =>
            {
                try
                {
                    act();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                    return;
                }
                tcs.TrySetResult(null);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            return tcs.Task;
        }
    }
}
