using System;
using System.Runtime.Serialization;

namespace NHibernate.Search.Cfg {
    [Serializable]
    public class AmbiguousSearchCfgException : Exception {
        private const string baseMessage = "An exception occurred during configuration of Search framework.";

        public AmbiguousSearchCfgException() {}

        public AmbiguousSearchCfgException(string message) : base(message) {}

        public AmbiguousSearchCfgException(string message, Exception inner) : base(message, inner) {}

        public AmbiguousSearchCfgException(Exception innerException)
            : base(baseMessage, innerException) {}

        protected AmbiguousSearchCfgException(SerializationInfo info, StreamingContext context)
            : base(info, context) {}
    }
}