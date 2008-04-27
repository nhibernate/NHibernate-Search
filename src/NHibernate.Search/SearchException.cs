using System;
using System.Runtime.Serialization;

namespace NHibernate.Search.Impl {
    [Serializable]
    public class SearchException : Exception {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SearchException() {}
        public SearchException(string message) : base(message) {}
        public SearchException(string message, Exception inner) : base(message, inner) {}

        protected SearchException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}