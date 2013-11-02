using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Communications.FailFast
{
    [Serializable]
    public class FailFastException : Exception
    {
        public FailFastException() : base(Strings.FailFastException_DefaultMessage) { }
        public FailFastException(string message) : base(message) { }
        public FailFastException(string message, Exception inner) : base(message, inner) { }
        protected FailFastException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
