namespace NHibernate.Search.Cfg
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The search configuration exception.
    /// </summary>
    [Serializable]
    public class SearchConfigurationException : Exception
    {
        private const string baseMessage = "An exception occurred during configuration of Search framework.";

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchConfigurationException"/> class.
        /// </summary>
        public SearchConfigurationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchConfigurationException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public SearchConfigurationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchConfigurationException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="inner">
        /// The inner.
        /// </param>
        public SearchConfigurationException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchConfigurationException"/> class.
        /// </summary>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        public SearchConfigurationException(Exception innerException) : base(baseMessage, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchConfigurationException"/> class.
        /// </summary>
        /// <param name="info">
        /// The info.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        protected SearchConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}