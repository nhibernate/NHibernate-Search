using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using log4net;
using Iesi.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.Search.Attributes;
using NHibernate.Search.Backend;
using NHibernate.Search.Cfg;
using NHibernate.Search.Engine;
using NHibernate.Search.Filter;
using NHibernate.Search.Reader;
using NHibernate.Search.Store;
using NHibernate.Search.Store.Optimization;
using NHibernate.Util;

namespace NHibernate.Search.Impl
{
    public class SearchFactoryImpl : ISearchFactoryImplementor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SearchFactoryImpl));
        private static readonly object searchFactoryKey = new object();

        //it's now a <Configuration, SearchFactory> map
        [ThreadStatic] private static WeakHashtable contexts;

        private readonly Dictionary<System.Type, DocumentBuilder> documentBuilders =
            new Dictionary<System.Type, DocumentBuilder>();

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

            Analyzer analyzer = InitAnalyzer(cfg);
            InitDocumentBuilders(cfg, analyzer);

            ISet<System.Type> classes = new HashedSet<System.Type>(documentBuilders.Keys);
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

        public Dictionary<System.Type, DocumentBuilder> DocumentBuilders
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

        private static Analyzer InitAnalyzer(Configuration cfg)
        {
            System.Type analyzerClass;

            String analyzerClassName = cfg.GetProperty(Environment.AnalyzerClass);
            if (analyzerClassName != null)
                try
                {
                    analyzerClass = ReflectHelper.ClassForName(analyzerClassName);
                }
                catch (Exception e)
                {
                    throw new SearchException(
                        string.Format("Lucene analyzer class '{0}' defined in property '{1}' could not be found.",
                                      analyzerClassName, Environment.AnalyzerClass), e);
                }
            else
                analyzerClass = typeof(StandardAnalyzer);
            // Initialize analyzer
            Analyzer defaultAnalyzer;
            try
            {
                defaultAnalyzer = (Analyzer) Activator.CreateInstance(analyzerClass);
            }
            catch (InvalidCastException)
            {
                throw new SearchException(
                    string.Format("Lucene analyzer does not implement {0}: {1}", typeof(Analyzer).FullName,
                                  analyzerClassName)
                    );
            }
            catch (Exception)
            {
                throw new SearchException("Failed to instantiate lucene analyzer with type " + analyzerClassName);
            }
            return defaultAnalyzer;
        }

        private void BindFilterDef(FullTextFilterDefAttribute defAnn, System.Type mappedClass)
        {
            if (filterDefinitions.ContainsKey(defAnn.Name))
                throw new SearchException("Multiple definitions of FullTextFilterDef.Name = " + defAnn.Name + ":" +
                                          mappedClass.FullName);

            FilterDef filterDef = new FilterDef();
            filterDef.Impl = defAnn.Impl;
            filterDef.Cache = defAnn.Cache;
            try
            {
                Activator.CreateInstance(filterDef.Impl);
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to create Filter class: " + filterDef.Impl.FullName, e);
            }

            foreach (MethodInfo method in filterDef.Impl.GetMethods())
            {
                if (AttributeUtil.HasAttribute<FactoryAttribute>(method))
                {
                    if (filterDef.FactoryMethod != null)
                        throw new SearchException("Multiple Factory methods found " + defAnn.Name + ":" +
                                                  filterDef.Impl.FullName + "." + method.Name);
                    filterDef.FactoryMethod = method;
                }
                if (AttributeUtil.HasAttribute<KeyAttribute>(method))
                {
                    if (filterDef.KeyMethod != null)
                        throw new SearchException("Multiple Key methods found " + defAnn.Name + ":" +
                                                  filterDef.Impl.FullName + "." + method.Name);
                    filterDef.KeyMethod = method;
                }
                // NB Don't need the setter logic that Java has
            }
            filterDefinitions[defAnn.Name] = filterDef;
        }

        private void BindFilterDefs(System.Type mappedClass)
        {
            // We only need one test here as we just support multiple FullTextFilter attributes rather than a collection
            foreach (FullTextFilterDefAttribute defAnn in AttributeUtil.GetAttributes<FullTextFilterDefAttribute>(mappedClass))
                BindFilterDef(defAnn, mappedClass);
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
            foreach (PersistentClass clazz in cfg.ClassMappings)
            {
                System.Type mappedClass = clazz.MappedClass;
                if (mappedClass != null)
                {
                    if (AttributeUtil.HasAttribute<IndexedAttribute>(mappedClass))
                    {
                        DirectoryProviderFactory.DirectoryProviders providers =
                            factory.CreateDirectoryProviders(mappedClass, cfg, this);

                        DocumentBuilder documentBuilder = new DocumentBuilder(mappedClass, analyzer, providers.Providers, providers.SelectionStrategy);

                        documentBuilders[mappedClass] = documentBuilder;
                    }
                    BindFilterDefs(mappedClass);
                }
            }
            factory.StartDirectoryProviders();
        }

        #endregion

        #region Public methods

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
                contexts = new WeakHashtable();
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
                lockableDirectoryProviders.Add(provider, new object());
        }

        public FilterDef GetFilterDefinition(string name)
        {
            return filterDefinitions[name];
        }

        public IOptimizerStrategy GetOptimizerStrategy(IDirectoryProvider provider)
        {
            return dirProviderOptimizerStrategy[provider];
        }

        public IDirectoryProvider[] GetDirectoryProviders(System.Type entity)
        {
            if (!documentBuilders.ContainsKey(entity))
                return null;
            DocumentBuilder documentBuilder = documentBuilders[entity];
            return documentBuilder.DirectoryProviders;
        }

        public void Optimize()
        {
            Dictionary<System.Type, DocumentBuilder>.KeyCollection clazzes = DocumentBuilders.Keys;
            foreach (System.Type clazz in clazzes)
                Optimize(clazz);
        }

        public void Optimize(System.Type entityType)
        {
            if (!DocumentBuilders.ContainsKey(entityType))
                throw new SearchException("Entity not indexed " + entityType);

            List<LuceneWork> queue = new List<LuceneWork>();
            queue.Add(new OptimizeLuceneWork(entityType));
            WaitCallback cb = BackendQueueProcessorFactory.GetProcessor(queue);
        }

        public Dictionary<IDirectoryProvider, object> GetLockableDirectoryProviders()
        {
            return lockableDirectoryProviders;
        }

        public void AddOptimizerStrategy(IDirectoryProvider provider, IOptimizerStrategy optimizerStrategy)
        {
            dirProviderOptimizerStrategy[provider] = optimizerStrategy;
        }

        public IFilterCachingStrategy GetFilterCachingStrategy()
        {
            return filterCachingStrategy;
        }

        public LuceneIndexingParameters GetIndexingParameters(IDirectoryProvider provider)
        {
            return dirProviderIndexingParams[provider];
        }

        public void AddIndexingParameters(IDirectoryProvider provider, LuceneIndexingParameters indexingParameters)
        {
            dirProviderIndexingParams[provider] = indexingParameters;
        }

        #endregion
    }
}