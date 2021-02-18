using System;
using System.Collections.Generic;
using System.Threading;

using Iesi.Collections.Generic;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Search.Backend;
using NHibernate.Search.Cfg;
using NHibernate.Search.Engine;
using NHibernate.Search.Filter;
using NHibernate.Search.Mapping;
using NHibernate.Search.Reader;
using NHibernate.Search.Store;
using NHibernate.Search.Store.Optimization;
using NHibernate.Search.Util;
using NHibernate.Util;

namespace NHibernate.Search.Impl
{
    public class SearchFactoryImpl : ISearchFactoryImplementor
    {
		private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(SearchFactoryImpl));
        private static readonly object searchFactoryKey = new object();

        private readonly ISearchMapping mapping;

        //it's now a <Configuration, SearchFactory> map
        [ThreadStatic] private static WeakHashtable contexts;

        private readonly TypeDictionary<DocumentBuilder> documentBuilders = new TypeDictionary<DocumentBuilder>(false);

        // Keep track of the index modifiers per DirectoryProvider since multiple entity can use the same directory provider
        private readonly Dictionary<IDirectoryProvider, object> lockableDirectoryProviders =
            new Dictionary<IDirectoryProvider, object>();

        private readonly Dictionary<IDirectoryProvider, IOptimizerStrategy> dirProviderOptimizerStrategy =
            new Dictionary<IDirectoryProvider, IOptimizerStrategy>();

        private readonly IWorker worker;
        private readonly IReaderProvider readerProvider;
        private IBackendQueueProcessorFactory backendQueueProcessorFactory;
        private readonly Dictionary<string, FilterDef> filterDefinitions = new Dictionary<string, FilterDef>();
        private IFilterCachingStrategy filterCachingStrategy;

        private int stopped;

        /*
         * Each directory provider (index) can have its own performance settings
         */

        private readonly Dictionary<IDirectoryProvider, LuceneIndexingParameters> dirProviderIndexingParams =
            new Dictionary<IDirectoryProvider, LuceneIndexingParameters>();

        #region Constructors

        private SearchFactoryImpl(Configuration cfg)
        {
            CfgHelper.Configure(cfg);

            mapping = SearchMappingFactory.CreateMapping(cfg);

            Analyzer analyzer = InitAnalyzer(cfg);
            InitDocumentBuilders(cfg, analyzer);

            ISet<System.Type> classes = new HashSet<System.Type>(documentBuilders.Keys);
            foreach (DocumentBuilder documentBuilder in documentBuilders.Values)
                documentBuilder.PostInitialize(classes);
            worker = WorkerFactory.CreateWorker(cfg, this);
            readerProvider = ReaderProviderFactory.CreateReaderProvider(cfg, this);
            BuildFilterCachingStrategy(cfg.Properties);
        }

        #endregion

        #region Property methods

        public IBackendQueueProcessorFactory BackendQueueProcessorFactory
        {
            get { return backendQueueProcessorFactory; }
            set { backendQueueProcessorFactory = value; }
        }

        public IDictionary<System.Type, DocumentBuilder> DocumentBuilders
        {
            get { return documentBuilders; }
        }

        public IFilterCachingStrategy FilterCachingStrategy
        {
            get { return filterCachingStrategy; }
        }

        public IReaderProvider ReaderProvider
        {
            get { return readerProvider; }
        }

        public IWorker Worker
        {
            get { return worker; }
        }

        #endregion

        #region Private methods

        private string GetProperty(IDictionary<string, string> props, string key)
        {
            return props.ContainsKey(key) ? props[key] : string.Empty;
        }

        private static Analyzer InitAnalyzer(Configuration cfg) => new StandardAnalyzer(LuceneVersion.LUCENE_48);
        // StandardAnalyzer requires at least a LuceneVersion as parameter
        // For the sake of simplicity, we are returning a new StandardAnalyzer, and ignoring any configured type
        //  This way, we can defer configuration of parameters until such a time as we require an analyzer other than the Standard
        //    --Ethan Eiter (February 18, 2021)

        //{
        //    System.Type analyzerClass;

        //    String analyzerClassName = cfg.GetProperty(Environment.AnalyzerClass);
        //    if (analyzerClassName != null)
        //        try
        //        {
        //            analyzerClass = ReflectHelper.ClassForName(analyzerClassName);
        //        }
        //        catch (Exception e)
        //        {
        //            throw new SearchException(
        //                string.Format("Lucene analyzer class '{0}' defined in property '{1}' could not be found.",
        //                              analyzerClassName, Environment.AnalyzerClass), e);
        //        }
        //    else
        //        analyzerClass = typeof(StandardAnalyzer);
        //    // Initialize analyzer
        //    Analyzer defaultAnalyzer;
        //    try
        //    {
        //        defaultAnalyzer = (Analyzer) Activator.CreateInstance(analyzerClass);
        //    }
        //    catch (InvalidCastException)
        //    {
        //        throw new SearchException(
        //            string.Format("Lucene analyzer does not implement {0}: {1}", typeof(Analyzer).FullName,
        //                          analyzerClassName)
        //            );
        //    }
        //    catch (Exception)
        //    {
        //        throw new SearchException("Failed to instantiate lucene analyzer with type " + analyzerClassName);
        //    }
        //    return defaultAnalyzer;
        //}

        private void BindFilterDefs(DocumentMapping mappedClass)
        {
            // We only need one test here as we just support multiple FullTextFilter attributes rather than a collection
            foreach (var filterDef in mappedClass.FullTextFilterDefinitions)
            {
                if (filterDefinitions.ContainsKey(filterDef.Name))
                    throw new SearchException("Multiple definitions of FullTextFilterDef.Name = " + filterDef.Name + ":" +
                                              mappedClass.MappedClass.FullName);


                filterDefinitions[filterDef.Name] = filterDef;
            }
        }

        private void BuildFilterCachingStrategy(IDictionary<string, string> properties)
        {
            string impl = GetProperty(properties, Environment.FilterCachingStrategy);
            if (string.IsNullOrEmpty(impl) || impl.ToUpperInvariant().Equals("MRU"))
            {
                filterCachingStrategy = new MruFilterCachingStrategy();
            }
            else
            {
                try
                {
                    filterCachingStrategy = (IFilterCachingStrategy)Activator.CreateInstance(ReflectHelper.ClassForName(impl));                    
                }
                catch (InvalidCastException)
                {
                    throw new SearchException("Class does not implement IFilterCachingStrategy: " + impl);
                }
                catch (Exception ex)
                {
                    throw new SearchException("Failed to instantiate IFilterCachingStrategy with type " + impl, ex);
                }
            }

            filterCachingStrategy.Initialize(properties);
        }

        private void InitDocumentBuilders(Configuration cfg, Analyzer analyzer)
        {
            DirectoryProviderFactory factory = new DirectoryProviderFactory();
            var classMappings = this.mapping.Build(cfg);

            foreach (var classMapping in classMappings)
            {
                System.Type mappedClass = classMapping.MappedClass;
                DirectoryProviderFactory.DirectoryProviders providers =
                    factory.CreateDirectoryProviders(classMapping, cfg, this);

                DocumentBuilder documentBuilder = new DocumentBuilder(classMapping, analyzer, providers.Providers, providers.SelectionStrategy);

                documentBuilders[mappedClass] = documentBuilder;
                BindFilterDefs(classMapping);
            }
            factory.StartDirectoryProviders();
        }

        #endregion

        #region Public methods

        public void AddOptimizerStrategy(IDirectoryProvider provider, IOptimizerStrategy optimizerStrategy)
        {
            dirProviderOptimizerStrategy[provider] = optimizerStrategy;
        }

        public void AddIndexingParameters(IDirectoryProvider provider, LuceneIndexingParameters indexingParameters)
        {
            dirProviderIndexingParams[provider] = indexingParameters;
        }

        public void Close()
        {
            if (Interlocked.Exchange(ref stopped, 1) == 0)
            {
                try
                {
                    readerProvider.Destroy();
                }
                catch (Exception e)
                {
                    log.Error("ReaderProvider raises an exception on destroy()", e);
                }
            }
        }

        public static SearchFactoryImpl GetSearchFactory(Configuration cfg)
        {
            if (contexts == null)
            {
                contexts = new WeakHashtable();
            }

            SearchFactoryImpl searchFactory = (SearchFactoryImpl) contexts[cfg];
            if (searchFactory == null)
            {
                searchFactory = new SearchFactoryImpl(cfg);
                contexts[cfg] = searchFactory;
            }
            return searchFactory;
        }

        public DocumentBuilder GetDocumentBuilder(object entity)
        {
            System.Type type = NHibernateUtil.GetClass(entity);
            return GetDocumentBuilder(type);
        }

        public DocumentBuilder GetDocumentBuilder(System.Type type)
        {
            DocumentBuilder builder;
            DocumentBuilders.TryGetValue(type, out builder);
            return builder;
        }

        public object GetLockObjForDirectoryProvider(IDirectoryProvider provider)
        {
            return lockableDirectoryProviders[provider];
        }

        public void PerformWork(object entity, object id, ISession session, WorkType workType)
        {
            Work work = new Work(entity, id, workType);
            worker.PerformWork(work, (ISessionImplementor) session);
        }

        public void RegisterDirectoryProviderForLocks(IDirectoryProvider provider)
        {
            if (lockableDirectoryProviders.ContainsKey(provider) == false)
            {
                lockableDirectoryProviders.Add(provider, new object());
            }
        }

        public bool TryGetFilterDefinition(string name, out FilterDef filter)
        {
            return filterDefinitions.TryGetValue(name, out filter);
        }

        public FilterDef GetFilterDefinition(string name)
        {
            return filterDefinitions[name];
        }

        public void AddFilterDefinition(string name, FilterDef filter)
        {
            filterDefinitions.Add(name, filter);
        }

        public IOptimizerStrategy GetOptimizerStrategy(IDirectoryProvider provider)
        {
            return dirProviderOptimizerStrategy[provider];
        }

        public LuceneIndexingParameters GetIndexingParameters(IDirectoryProvider provider)
        {
            return dirProviderIndexingParams[provider];
        }

        public IDirectoryProvider[] GetDirectoryProviders(System.Type entity)
        {
            if (!documentBuilders.ContainsKey(entity))
            {
                return null;
            }
            DocumentBuilder documentBuilder = documentBuilders[entity];
            return documentBuilder.DirectoryProviders;
        }

        public void Optimize()
        {
            var clazzes = DocumentBuilders.Keys;
            foreach (System.Type clazz in clazzes)
            {
                Optimize(clazz);
            }
        }

        public void Optimize(System.Type entityType)
        {
            if (!DocumentBuilders.ContainsKey(entityType))
            {
                throw new SearchException("Entity not indexed " + entityType);
            }

            List<LuceneWork> queue = new List<LuceneWork>();
            queue.Add(new OptimizeLuceneWork(entityType));
            WaitCallback cb = BackendQueueProcessorFactory.GetProcessor(queue);
            cb(null);
        }

        public Dictionary<IDirectoryProvider, object> GetLockableDirectoryProviders()
        {
            return lockableDirectoryProviders;
        }

        public IFilterCachingStrategy GetFilterCachingStrategy()
        {
            return filterCachingStrategy;
        }

        #endregion
    }
}