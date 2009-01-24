namespace NHibernate.Search
{
    /// <summary>
    /// Extends the NHibernate <see cref="ISession" /> with full text search and indexing capabilities
    /// </summary>
    public interface IFullTextSession : ISession
    {
        IFullTextQuery CreateFullTextQuery<TEntity>(string defaultField, string query);

        IFullTextQuery CreateFullTextQuery<TEntity>(string query);

        /// <summary>
        /// Create a <see cref="IQuery" /> on top of a native Lucene <see cref="Lucene.Net.Search.Query" /> returning
        /// the matching object of type <c>entities</c> and their respective subbclasses.
        /// If no entity is provided, no type filtering is done.
        /// </summary>
        /// <param name="luceneQuery"></param>
        /// <param name="entities"></param>
        /// <returns></returns>
        IFullTextQuery CreateFullTextQuery(Lucene.Net.Search.Query luceneQuery, params System.Type[] entities);

        /// <summary>
        /// Force the (re)indexing of a given <b>managed</b> object.
        /// Indexation is batched per transaction</summary>
        /// <param name="entity"></param>
        IFullTextSession Index(object entity);

        /// <summary>
        /// Purge the instance with the specified identity from the index, but not the database.
        /// </summary>
        /// <param name="clazz"></param>
        /// <param name="id"></param>
        void Purge(System.Type clazz, object id);

        /// <summary>
        /// Purge all instances from the index, but not the database.
        /// </summary>
        /// <param name="clazz"></param>
        void PurgeAll(System.Type clazz);
    }
}