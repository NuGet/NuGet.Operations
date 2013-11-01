using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.Communications
{
    public abstract class ServiceFailureDetector
    {
        /// <summary>
        /// Gets the default failure detector which considers all exceptions failures and all results successes
        /// </summary>
        public static ServiceFailureDetector Default = new DefaultFailureDetector();

        public abstract bool IsFailureException(Exception ex);

        /// <summary>
        /// Determines if the provided result is considered a failure. NOTE: Result may be null if the action
        /// either does not return a value or returned null!
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public abstract bool IsFailureResult(object result);

        private class DefaultFailureDetector : ServiceFailureDetector
        {
            public override bool IsFailureException(Exception ex)
            {
                return true;
            }

            public override bool IsFailureResult(object result)
            {
                return false;
            }
        }
    }

    public abstract class ServiceFailureDetector<TResultType> : ServiceFailureDetector
    {
        public override bool IsFailureResult(object result)
        {
            return IsFailureResult((TResultType)result);
        }

        public abstract bool IsFailureResult(TResultType result);
    }
}
