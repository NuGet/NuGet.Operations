using System;
using System.Runtime.Serialization;

namespace FormatProcessor {
    [Serializable]
    public class FormatException : Exception {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public FormatException() {}
        public FormatException(string message) : base(message) {}
        public FormatException(string message, Exception inner) : base(message, inner) {}

        protected FormatException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}