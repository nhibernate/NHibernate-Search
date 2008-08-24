namespace NHibernate.Search
{
    /// <summary>
    /// Represents a full text filter that is about to be applied.
    /// Used to inject parameters
    /// </summary>
    public interface IFullTextFilter
    {
        IFullTextFilter SetParameter(string name, object value);
        object GetParameter(string name);
    }
}