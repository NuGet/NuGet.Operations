using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services
{
    public class SpyFacts
    {
        [Fact]
        public void SpyInvokesActionWithoutThrowing()
        {
            var spy = new Spy<Action>();
            spy.Delegate();
        }

        [Fact]
        public void SpyReturnsDefaultValueForReturnValue()
        {
            var spy = new Spy<Func<int>>();
            Assert.Equal(default(int), spy.Delegate());
        }

        [Fact]
        public void SpyCollectsProvidedParameters()
        {
            // Arrange
            var spy = new Spy<Action<int>>();

            // Act
            spy.Delegate(42);
            
            // Assert
            Assert.True(spy.WasCalledWith(42));
            Assert.False(spy.WasCalledWith(24));
        }

        [Fact]
        public void SpyReturnsValueProvidedInAlwaysReturns()
        {
            // Arrange
            var spy = new Spy<Func<int, int>>();
            spy.AlwaysReturns(84);

            // Act/Assert
            Assert.Equal(84, spy.Delegate(42));
        }

        [Fact]
        public void SpyIsImplicitlyConvertableToDelegate()
        {
            // Arrange
            var spy = new Spy<Func<int, string>>();
            spy.AlwaysReturns("foo");

            // Act/Assert
            Assert.Equal("foo", Invoker(spy, 42));
        }

        private string Invoker(Func<int, string> act, int val)
        {
            return act(val);
        }
    }
}
