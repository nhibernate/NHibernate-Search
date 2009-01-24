using Lucene.Net.Search;

namespace NHibernate.Search
{
    using Attributes;

    using Bridge;

    using Transform;

    /// <summary>
    /// The base interface for lucene powered searches.
    /// </summary>
    public interface IFullTextQuery : IQuery
    {        
        /// <summary>
        /// Returns the number of hits for this search
        /// </summary>
        /// <remarks>
        /// Caution:
        /// The number of results might be slightly different from
        ///<code>List().size()</code> because List() if the index is
        /// not in sync with the database at the time of query.
        /// </remarks>
        int ResultSize { get; }

        /// <summary>
        /// Allows to let lucene sort the results. This is useful when you have
        /// additional sort requirements on top of the default lucene ranking.
        /// Without lucene sorting you would have to retrieve the full result set and
        /// order the hibernate objects.
        /// </summary>
        /// <param name="sort">The lucene sort object.</param>
        /// <returns>this for method chaining</returns>
        IFullTextQuery SetSort(Sort sort);

        /// <summary>
        /// Allows to use lucene filters.
        /// Semi-deprecated? a preferred way is to use the [FullTextFilterDef] attribute approach.
        /// </summary>
        /// <param name="filter">The lucene filter.</param>
        /// <returns>this for method chaining</returns>
        IFullTextQuery SetFilter(Lucene.Net.Search.Filter filter);

        /// <summary>
        /// <para>
        /// Defines the Database Query used to load the Lucene results.
        /// Useful to load a given object graph by refining the fetch modes.
        /// </para>
        /// No projection (criteria.SetProjection() ) allowed, the root entity must be the only returned type
        /// No where restriction can be defined either.
        /// </summary>
        /// <param name="criteria">The criteria to apply.</param>
        /// <returns>this for method chaining</returns>
        IFullTextQuery SetCriteriaQuery(ICriteria criteria);

        /// <summary>
        /// Defines the Lucene field names projected and returned in a query result
        /// Each field is converted back to it's object representation, an object[] being returned for each "row"
        /// (similar to an HQL or a Criteria API projection).
        /// <para>
        /// A projectable field must be stored in the Lucene index and use a <see cref="ITwoWayFieldBridge" />
        /// Unless notified in their documentation, all built-in bridges are two-way. All <see cref="DocumentIdAttribute" /> fields are projectable by design.
        /// </para>
        /// If the projected field is not a projectable field, null is returned in the object[]
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IFullTextQuery SetProjection(params string[] fields);

        /// <summary>
        /// Enable a given filter by its name. Returns a <see cref="IFullTextFilter" /> object that allows filter parameter injection
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IFullTextFilter EnableFullTextFilter(string name);

        /// <summary>
        /// Disable a given filter by its name
        /// </summary>
        /// <param name="name"></param>
        void DisableFullTextFilter(string name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstResult"></param>
        /// <returns></returns>
        new IFullTextQuery SetFirstResult(int firstResult);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxResults"></param>
        /// <returns></returns>
        new IFullTextQuery SetMaxResults(int maxResults);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fetchSize"></param>
        /// <returns></returns>
        IFullTextQuery SetFetchSize(int fetchSize);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transformer"></param>
        /// <returns></returns>
        new IFullTextQuery SetResultTransformer(IResultTransformer transformer);
    }
}