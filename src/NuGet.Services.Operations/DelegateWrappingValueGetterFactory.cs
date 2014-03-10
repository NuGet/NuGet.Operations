using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nustache.Core;

namespace NuGet.Services.Operations
{
    class DelegateWrappingValueGetterFactory : ValueGetterFactory
    {
        private ValueGetterFactoryCollection _factories = new ValueGetterFactoryCollection();

        public DelegateWrappingValueGetterFactory(IEnumerable<ValueGetterFactory> factories)
        {
            foreach (var factory in factories)
            {
                _factories.Add(factory);
            }
        }

        public override ValueGetter GetValueGetter(object target, Type targetType, string name)
        {
            var value = _factories.GetValueGetter(target, name);
            return new WrappedValueGetter(value);
        }

        private class WrappedValueGetter : ValueGetter
        {
            private ValueGetter _getter;

            public WrappedValueGetter(ValueGetter getter)
            {
                _getter = getter;
            }

            public override object GetValue()
            {
                var inner = _getter.GetValue();
                Func<object> func = inner as Func<object>;
                if (func != null)
                {
                    return func();
                }
                else
                {
                    return inner;
                }
            }
        }
    }
}
