using Lucene.Net.Search;

namespace NHibernate.Search
{
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
    }
}