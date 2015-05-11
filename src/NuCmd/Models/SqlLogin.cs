// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuCmd.Models
{
    public class SqlLogin : SqlPrincipal
    {
        public string name { get; set; }
        public DateTime create_date { get; set; }
        public DateTime modify_date { get; set; }
    }
}
