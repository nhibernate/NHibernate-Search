using System.Collections.Generic;
using NHibernate.Cfg;

namespace NHibernate.Search.Cfg {
    /// <summary>
    /// Contract for configuration sources.
    /// </summary>
    public interface INHSConfiguration {
        /// <summary>
        /// Configured properties
        /// </summary>
        IDictionary<string, string> Properties { get; }

        /// <summary>
        /// If property exists in the <see cref="Properties"/> dictionary, it returns the value.
        /// Otherwise, GetProperty will return null.
        /// </summary>
        string GetProperty(string name);

        IDictionary<string, string> GetMergedProperties(Configuration cfg);
    }
}