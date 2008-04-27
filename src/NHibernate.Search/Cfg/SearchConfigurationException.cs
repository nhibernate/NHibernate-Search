using System;
using System.Runtime.Serialization;

namespace NHibernate.Search.Cfg {
    [Serializable]
    public class SearchConfigurationException : Exception {
        private const string baseMessage = "An exception occurred during configuration of Search framework.";

        public SearchConfigurationException() {}

        public SearchConfigurationException(string message) : base(message) {}

        public SearchConfigurationException(string message, Exception inner) : base(message, inner) {}

        public SearchConfigurationException(Exception innerException)
            : base(baseMessage, innerException) {}

        protected SearchConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context) {}
    }
}