using NHibernate.Search.Engine;
using NHibernate.Search.Reader;
using NHibernate.Search.Store;

namespace NHibernate.Search
{
    /// <summary>
    /// Provide application wide operations as well as access to the underlying Lucene resources.
    /// </summary>
    public interface ISearchFactory
    {
        /// <summary>
        /// Provide the configured readerProvider strategy,
        /// hence access to a Lucene IndexReader
        /// </summary>
        IReaderProvider ReaderProvider { get; }

        /// <summary>
        /// Provide access to the DirectoryProviders (hence the Lucene Directories)
        /// for a given entity
        /// In most cases, the returned type will be a one element array.
        /// But if the given entity is configured to use sharded indexes, then multiple
        /// elements will be returned. In this case all of them should be considered.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        IDirectoryProvider[] GetDirectoryProviders(System.Type entity);

        /// <summary>
        /// Optimize all indexes
        /// </summary>
        void Optimize();

        /// <summary>
        /// Optimize the index holding <code>entityType</code>
        /// </summary>
        /// <param name="entityType"></param>
        void Optimize(System.Type entityType);

        /// <summary>
        /// Gets a FilterDef object by name from the ISearchFactory implementation. 
        /// A return value indicates if a matching FilterDef was found
        /// </summary>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        /// <returns>True/false whether or not a FilterDef exists for the name.</returns>
        bool TryGetFilterDefinition(string name, out FilterDef filter);

        /// <summary>
        /// Gets a FilterDef object by name from the ISearchFactory implementation. 
        /// Throws an exception if one does not exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A FilterDef object associated with the included name parameter.</returns>
        FilterDef GetFilterDefinition(string name);
    }
}