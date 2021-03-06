﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuCmd.Models
{
    public class SqlPermission : SqlPrincipal
    {
        public int principal_id { get; set; }
        public string name { get; set; }
        public string class_desc { get; set; }
        public string object_name { get; set; }
        public string permission_name { get; set; }
        public string state_desc { get; set; }
    }
}
