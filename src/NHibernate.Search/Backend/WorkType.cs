namespace NHibernate.Search.Backend
{
    /// <summary>
    /// Enumeration of different types of Lucene work. This enumeration is used to specify the type of
    /// index operation to be executed.
    /// </summary>
    public enum WorkType
    {
        Add,
        Update,
        Delete,
        Collection,
        /// <summary>
        /// Used to remove a specific instance of a class from an index.
        /// </summary>
        Purge,
        /// <summary>
        /// Used to remove all instances of a class from an index.
        /// </summary>
        PurgeAll,
        /// <summary>
        /// Used for batch indexing.
        /// </summary>
        Index
    }
}