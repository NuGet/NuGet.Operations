using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Operations.Model;
using RazorEngine;
using RazorEngine.Templating;

namespace NuGet.Services.Operations.Config
{
    public class ConfigTemplateBase : TemplateBase<ConfigTemplateModel>
    {
        public dynamic resources { get { return Model.Resources; } }
        public Service service { get { return Model.Service; } }

        public override void WriteAttribute(string name, PositionTagged<string> prefix, PositionTagged<string> suffix, params AttributeValue[] values)
        {
            values = values.Select(val => new AttributeValue(
                val.Prefix,
                new PositionTagged<object>(FilterNull(val.Value.Value), val.Value.Position),
                val.Literal)).ToArray();

            base.WriteAttribute(name, prefix, suffix, values);
        }

        public override void Write(object value)
        {
            base.Write(FilterNull(value));
        }

        private object FilterNull(object p)
        {
            return ConfigObject.IsNullObject(p) ? String.Empty : p;
        }
    }
}
