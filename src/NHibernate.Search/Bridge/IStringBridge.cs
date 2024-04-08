namespace NHibernate.Search.Bridge
{
    /// <summary>
    /// Transform an object into a string representation
    /// </summary>
    public interface IStringBridge
    {
        /// <summary>
        /// Convert the object representation to a String
        /// The return String must not be null, it can be empty though</summary>
        /// <param name="?"></param>
        string ObjectToString(object obj);
    }
}