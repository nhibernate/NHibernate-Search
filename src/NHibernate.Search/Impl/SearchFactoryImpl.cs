using System;
using System.Collections;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.Search.Backend;
using NHibernate.Search.Backend.Impl;
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
        private static readonly object searchFactoryKey = new object();

        [ThreadStatic] private static WeakHashtable cfg2SearchFactory = new WeakHashtable();
        //it's now a <Configuration, SearchFactory> map

        private readonly Dictionary<System.Type, DocumentBuilder> documentBuilders =
            new Dictionary<System.Type, DocumentBuilder>();

        /// <summary>
        /// Note that we will lock on the values in this dictionary
        /// </summary>
        private readonly Dictionary<IDirectoryProvider, object> lockableDirectoryProviders =
            new Dictionary<IDirectoryProvider, object>();

        private readonly IQueueingProcessor queueingProcessor;
        private readonly IWorker worker;
        private IBackendQueueProcessorFactory backendQueueProcessorFactory;

        #region Constructors

        private SearchFactoryImpl(Configuration cfg)
        {
            CfgHelper.Configure(cfg);
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
                                      analyzerClassName, Environment.AnalyzerClass),
                        e
                        );
                }
            else
                analyzerClass = typeof(StandardAnalyzer);
            // Initialize analyzer
            Analyzer analyzer;
            try
            {
                analyzer = (Analyzer) Activator.CreateInstance((System.Type) analyzerClass);
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
            queueingProcessor = new BatchedQueueingProcessor(this, (IDictionary) cfg.Properties);

            DirectoryProviderFactory factory = new DirectoryProviderFactory();

            foreach (PersistentClass clazz in cfg.ClassMappings)
            {
                System.Type mappedClass = clazz.MappedClass;
                if (mappedClass != null && AttributeUtil.IsIndexed(mappedClass))
                {
                    IDirectoryProvider provider = factory.CreateDirectoryProvider(mappedClass, cfg, this);

                    DocumentBuilder documentBuilder = new DocumentBuilder(mappedClass, analyzer, provider);

                    documentBuilders.Add(mappedClass, documentBuilder);
                }
            }
            ISet<System.Type> classes = new HashedSet<System.Type>(documentBuilders.Keys);
            foreach (DocumentBuilder documentBuilder in documentBuilders.Values)
                documentBuilder.PostInitialize(classes);
            worker = WorkerFactory.CreateWorker(cfg, this);
        }

        #endregion

        #region Property methods

        public Dictionary<System.Type, DocumentBuilder> DocumentBuilders
        {
            get { return documentBuilders; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IBackendQueueProcessorFactory BackendQueueProcessorFactory
        {
            get { return backendQueueProcessorFactory; }
            set { backendQueueProcessorFactory = value; }
        }

        #endregion

        #region Public methods

        public IDirectoryProvider GetDirectoryProvider(System.Type entity)
        {
            return GetDocumentBuilder(entity).DirectoryProvider;
        }

        public static SearchFactoryImpl GetSearchFactory(Configuration cfg)
        {
            if (cfg2SearchFactory == null)
                cfg2SearchFactory = new WeakHashtable();
            SearchFactoryImpl searchFactory = (SearchFactoryImpl) cfg2SearchFactory[cfg];
            if (searchFactory == null)
            {
                searchFactory = new SearchFactoryImpl(cfg);
                cfg2SearchFactory[cfg] = searchFactory;
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

        #endregion

        #region ISearchFactoryImplementor Members

        public IWorker Worker
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public FilterDef GetFilterDefinition(string name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IOptimizerStrategy GetOptimizerStrategy(IDirectoryProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IReaderProvider ReaderProvider
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public IDirectoryProvider[] GetDirectoryProviders(System.Type entity)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Optimize()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Optimize(System.Type entityType)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Dictionary<IDirectoryProvider, object> GetLockableDirectoryProviders()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void AddOptimizerStrategy(IDirectoryProvider provider, IOptimizerStrategy optimizerStrategy)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IFilterCachingStrategy GetFilterCachingStrategy()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public LuceneIndexingParameters GetIndexingParameters(IDirectoryProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void AddIndexingParameters(IDirectoryProvider provider, LuceneIndexingParameters indexingParameters)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}