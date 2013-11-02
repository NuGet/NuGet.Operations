using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services
{
    public static class AsyncAssert
    {
        public static async Task<TException> Throws<TException>(Func<Task> operation) where TException : Exception
        {
            Exception thrown = null;
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            Assert.True(thrown != null, String.Format("Expected that a {0} exception would be thrown, but none was thrown.", typeof(TException).FullName));

            TException expected = thrown as TException;
            Assert.True(expected != null, String.Format("Expected that a {0} exception would be thrown, but a {1} was thrown instead.", typeof(TException).FullName, thrown.GetType().FullName));

            return expected;
        }
    }
}
