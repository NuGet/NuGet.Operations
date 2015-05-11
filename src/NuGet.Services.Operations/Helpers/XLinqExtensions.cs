// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Xml.Linq
{
    internal static class XLinqExtensions
    {
        public static string AttributeValueOrDefault(this XElement self, XName name)
        {
            return AttributeValueOrDefault(self, name, String.Empty);
        }

        private static string AttributeValueOrDefault(XElement self, XName name, string defaultValue)
        {
            return AttributeValueOrDefault<string>(
                self,
                name,
                s => s,
                defaultValue);
        }

        public static T AttributeValueOrDefault<T>(this XElement self, XName name, Func<string, T> converter)
        {
            return AttributeValueOrDefault(self, name, converter, default(T));
        }

        public static T AttributeValueOrDefault<T>(this XElement self, XName name, Func<string, T> converter, T defaultValue)
        {
            var attr = self.Attribute(name);
            if (attr == null)
            {
                return defaultValue;
            }
            return converter(attr.Value);
        }
    }
}
