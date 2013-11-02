using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NuGet.Services
{
    public class Spy<T>
    {
        private List<SpyCall> _calls = new List<SpyCall>();

        private object _returns = null;
        
        public readonly T Delegate;

        public Expression Body { get; private set; }
        public IReadOnlyList<SpyCall> Calls
        {
            get { return _calls.AsReadOnly(); }
        }

        public Spy()
        {
            Delegate = GenerateDelegate();
        }

        public static implicit operator T(Spy<T> instance)
        {
            return instance.Delegate;
        }

        private T GenerateDelegate()
        {
            var type = typeof(T);
            if (!typeof(MulticastDelegate).IsAssignableFrom(type) && type != typeof(MulticastDelegate))
            {
                throw new InvalidOperationException("Spy type must be a delegate!");
            }

            var invokeMethod = type.GetMethod("Invoke");
            var parameters = invokeMethod.GetParameters();

            var paramExprs = parameters.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToList();
            var arr = Expression.NewArrayInit(typeof(SpyParameter), paramExprs.Select(p => 
                Expression.New(
                    SpyParameter.Ctor,
                    Expression.Constant(p.Name),
                    Expression.Convert(p, typeof(object)))));

            var spyInstance = Expression.Constant(this);
            var addCallMethod = typeof(Spy<T>).GetMethod(
                "InternalAddCall", 
                BindingFlags.NonPublic | BindingFlags.Instance, 
                Type.DefaultBinder, 
                new[] { typeof(IEnumerable<SpyParameter>) }, 
                new ParameterModifier[0]);

            Body = Expression.Call(spyInstance, addCallMethod, arr);

            if (invokeMethod.ReturnType != typeof(void))
            {
                var returns = Expression.Field(
                    Expression.Constant(this),
                    "_returns");
                Body = Expression.Block(
                    Body,
                    Expression.Condition(
                        Expression.Equal(returns, Expression.Constant(null)),
                        Expression.Default(invokeMethod.ReturnType),
                        Expression.Convert(returns, invokeMethod.ReturnType),
                        invokeMethod.ReturnType));
            }
            var lambda = Expression.Lambda<T>(Body, paramExprs);
            return lambda.Compile();
        }

        public void AlwaysReturns(object value)
        {
            _returns = value;
        }

        /// <summary>
        /// Designed to be used from the generated delegate. Do not call manually
        /// </summary>
        /// <param name="call"></param>
        internal void InternalAddCall(IEnumerable<SpyParameter> parameters)
        {
            _calls.Add(new SpyCall(parameters));
        }

        public bool WasCalledWith(params object[] args)
        {
            return _calls.Any(c => c.Matches(args));
        }
    }

    public class SpyCall
    {
        private IDictionary<string, object> _parameterValues;

        public IList<SpyParameter> Parameters { get; private set; }

        public object this[string name] { get { return _parameterValues[name]; } }
        public object this[int index] { get { return Parameters[index]; } }

        internal SpyCall(IEnumerable<SpyParameter> parameters)
        {
            Parameters = parameters.ToList();
            _parameterValues = parameters.ToDictionary(p => p.Name, p => p.Value);
        }

        internal bool Matches(object[] args)
        {
            return Enumerable.SequenceEqual(
                Parameters.Select(p => p.Value),
                args);
        }
    }

    public class SpyParameter
    {
        internal static readonly ConstructorInfo Ctor = typeof(SpyParameter).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            Type.DefaultBinder,
            new[] { typeof(string), typeof(object) },
            new ParameterModifier[0]);

        public string Name { get; private set; }
        public object Value { get; private set; }

        internal SpyParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
