namespace NHibernate.Search.Cfg
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The ambiguous search cfg exception.
    /// </summary>
    [Serializable]
    public class AmbiguousSearchCfgException : Exception
    {
        private const string baseMessage = "An exception occurred during configuration of Search framework.";

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbiguousSearchCfgException"/> class.
        /// </summary>
        public AmbiguousSearchCfgException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbiguousSearchCfgException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public AmbiguousSearchCfgException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbiguousSearchCfgException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="inner">
        /// The inner.
        /// </param>
        public AmbiguousSearchCfgException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbiguousSearchCfgException"/> class.
        /// </summary>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        public AmbiguousSearchCfgException(Exception innerException) : base(baseMessage, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbiguousSearchCfgException"/> class.
        /// </summary>
        /// <param name="info">
        /// The info.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        protected AmbiguousSearchCfgException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}