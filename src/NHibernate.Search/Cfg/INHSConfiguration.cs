namespace NHibernate.Search.Cfg
{
    using System.Collections.Generic;

    /// <summary>
    /// Contract for configuration sources.
    /// </summary>
    public interface INHSConfiguration
    {
        /// <summary>
        /// Configured properties
        /// </summary>
        IDictionary<string, string> Properties { get; }

        /// <summary>
        /// Gets SessionFactoryName.
        /// </summary>
        string SessionFactoryName { get; }

        /// <summary>
        /// If property exists in the <see cref="Properties"/> dictionary, it returns the value.
        /// Otherwise, GetProperty will return null.
        /// </summary>
        /// <param name="name">
        /// The name of the property.
        /// </param>
        /// <returns>
        /// The property value.
        /// </returns>
        string GetProperty(string name);
    }
}