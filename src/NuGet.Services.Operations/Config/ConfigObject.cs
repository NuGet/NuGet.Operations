using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Operations.Config
{
    public class ConfigObject : DynamicObject
    {
        public IDictionary<string, object> Items { get; private set; }

        public ConfigObject(IDictionary<string, object> items)
        {
            Items = new Dictionary<string, object>(items, StringComparer.OrdinalIgnoreCase);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return Items.Keys;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.ReturnType == typeof(IDictionary<string, object>))
            {
                result = Items;
                return true;
            }

            return base.TryConvert(binder, out result);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length == 1)
            {
                string key = indexes[0] as string;
                if (key != null)
                {
                    result = GetOrDefault(key);
                    return true;
                }
            }

            return base.TryGetIndex(binder, indexes, out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = GetOrDefault(binder.Name);
            return true;
        }

        public static bool IsNullObject(object obj)
        {
            return obj is NullObject;
        }

        private object GetOrDefault(string key)
        {
            object ret;
            if (!Items.TryGetValue(key, out ret))
            {
                return NullObject.Instance;
            }
            return ConvertValue(ret);
        }

        private object ConvertValue(object ret)
        {
            IDictionary<string, object> dict = ret as IDictionary<string, object>;
            if (dict != null)
            {
                return new ConfigObject(dict);
            }
            else if (!(ret is string))
            {
                IEnumerable enumer = ret as IEnumerable;
                if (enumer != null)
                {
                    return enumer.OfType<object>().Select(o => ConvertValue(o));
                }
            }
            return ret;
        }

        private class NullObject : DynamicObject
        {
            public static readonly NullObject Instance = new NullObject();
            private NullObject() { }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return Enumerable.Empty<string>();
            }

            public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
            {
                result = this;
                return true;
            }

            public override bool TryConvert(ConvertBinder binder, out object result)
            {
                result = this;
                return true;
            }

            public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
            {
                result = this;
                return true;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                result = this;
                return true;
            }

            public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
            {
                result = this;
                return true;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                result = this;
                return true;
            }

            public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
            {
                return true;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                return true;
            }

            public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
            {
                result = this;
                return true;
            }
        }
    }
}
