using System.Collections.Generic;
using NHibernate.Search.Backend;
using NHibernate.Search.Filter;
using NHibernate.Search.Store;
using NHibernate.Search.Store.Optimization;

namespace NHibernate.Search.Engine
{
    /// <summary>
    /// Interface which gives access to the different directory providers and their configuration.
    /// </summary>
    public interface ISearchFactoryImplementor : ISearchFactory
    {
        IBackendQueueProcessorFactory BackendQueueProcessorFactory { get; set; }

        IDictionary<System.Type, DocumentBuilder> DocumentBuilders { get; }

        Dictionary<IDirectoryProvider, object> GetLockableDirectoryProviders();

        IWorker Worker { get; }

        void AddOptimizerStrategy(IDirectoryProvider provider, IOptimizerStrategy optimizerStrategy);

        IOptimizerStrategy GetOptimizerStrategy(IDirectoryProvider provider);

        IFilterCachingStrategy GetFilterCachingStrategy();

        LuceneIndexingParameters GetIndexingParameters(IDirectoryProvider provider);

        void AddIndexingParameters(IDirectoryProvider provider, LuceneIndexingParameters indexingParameters);

        void Close();

        /// <summary>
        /// Adds a FilterDef object to the ISearchFactory implementation with the given name.
        /// In most cases, FilterDefs should be added during mapping configuration in a 
        /// custom ISearchMapping implementation. This method enables FilterDefs to be added 
        /// after mapping at run-time anytime an IFullTextSession is available. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        void AddFilterDefinition(string name, FilterDef filter);
    }
}