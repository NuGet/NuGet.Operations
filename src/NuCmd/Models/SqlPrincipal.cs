// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuCmd.Models
{
    public class SqlPrincipal
    {
        public byte[] sid { get; set; }

        public string sid_string
        {
            get { return sid == null ? String.Empty : Convert.ToBase64String(sid); }
        }
    }
}
