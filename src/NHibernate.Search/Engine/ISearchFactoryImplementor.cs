using System.Collections.Generic;
using NHibernate.Search.Backend;
using NHibernate.Search.Filter;
using NHibernate.Search.Storage;
using NHibernate.Search.Store.Optimization;

namespace NHibernate.Search.Engine
{
    /// <summary>
    /// Interface which gives access to the different directory providers and their configuration.
    /// </summary>
    public interface ISearchFactoryImplementor : ISearchFactory
    {
        IBackendQueueProcessorFactory BackendQueueProcessorFactory { get; set; }

        IWorker Worker { get; }

        Dictionary<System.Type, DocumentBuilder> DocumentBuilders { get; }

        Dictionary<IDirectoryProvider, object> GetLockableDirectoryProviders();

        void AddOptimizerStrategy(IDirectoryProvider provider, IOptimizerStrategy optimizerStrategy);

        IOptimizerStrategy GetOptimizerStrategy(IDirectoryProvider provider);

        IFilterCachingStrategy GetFilterCachingStrategy();

        FilterDef GetFilterDefinition(string name);

        IDirectoryProvider GetDirectoryProvider(System.Type entity);

        LuceneIndexingParameters GetIndexingParameters(IDirectoryProvider provider);

        void AddIndexingParameters(IDirectoryProvider provider, LuceneIndexingParameters indexingParameters);
    }
}