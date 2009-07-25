namespace NHibernate.Search
{
    /// <summary>
    /// Represents a full text filter that is about to be applied.
    /// Used to inject parameters
    /// </summary>
    public interface IFullTextFilter
    {
        /// <summary>
        /// Assigns a parameter to the filter.
        /// <para>
        /// The .NET version differs from java, we do not locate setter methods, but find properties
        /// that have the FilterParameter attribute set.
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IFullTextFilter SetParameter(string name, object value);

        /// <summary>
        /// Returns the value for a named parameter.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object GetParameter(string name);
    }
}