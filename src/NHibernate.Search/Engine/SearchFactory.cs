using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.Search.Backend;
using NHibernate.Search.Backend.Impl;
using NHibernate.Search.Impl;
using NHibernate.Search.Storage;
using NHibernate.Util;

namespace NHibernate.Search.Engine
{
	public class SearchFactory   
    {
		[ThreadStatic]
		private static WeakHashtable cfg2SearchFactory = new WeakHashtable(); //it's now a <Configuration, SearchFactory> map
        private static readonly object searchFactoryKey = new object();

        /// <summary>
        /// Note that we will lock on the values in this dictionary
        /// </summary>
        private readonly Dictionary<IDirectoryProvider, object> lockableDirectoryProviders = new Dictionary<IDirectoryProvider, object>();
        private readonly Dictionary<System.Type, DocumentBuilder> documentBuilders = new Dictionary<System.Type, DocumentBuilder>();
        private readonly IQueueingProcessor queueingProcessor;
        private IBackendQueueProcessorFactory backendQueueProcessorFactory;
		private IWorker worker;

        public Dictionary<System.Type, DocumentBuilder> DocumentBuilders
        {
            get { return documentBuilders; }
        }

		

 
		public static SearchFactory GetSearchFactory(Configuration cfg)
        {
			if (cfg2SearchFactory == null)
				cfg2SearchFactory = new WeakHashtable();
			SearchFactory searchFactory = (SearchFactory) cfg2SearchFactory[cfg];
			if (searchFactory == null) {
				searchFactory = new SearchFactory(cfg);
				cfg2SearchFactory[cfg] = searchFactory;
			}
			return searchFactory;
        } 

		private SearchFactory(Configuration cfg)
        {
			Cfg.CfgHelper.Config(cfg);
            System.Type analyzerClass;

            String analyzerClassName = cfg.GetProperty(Environment.AnalyzerClass);
            if (analyzerClassName != null)
            {
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
            }
            else
            {
                analyzerClass = typeof(StandardAnalyzer);
            }
            // Initialize analyzer
            Analyzer analyzer;
            try
            {
                analyzer = (Analyzer) Activator.CreateInstance(analyzerClass);
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
            queueingProcessor = new BatchedQueueingProcessor(this, (System.Collections.IDictionary)cfg.Properties);

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
            {
                documentBuilder.PostInitialize(classes);
            }
			worker = WorkerFactory.CreateWorker(cfg, this);
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

        public IDirectoryProvider GetDirectoryProvider(System.Type entity)
        {
            return GetDocumentBuilder(entity).DirectoryProvider;
        }

        public object GetLockObjForDirectoryProvider(IDirectoryProvider provider)
        {
            return lockableDirectoryProviders[provider];
        }

        public void PerformWork(object entity, object id, ISession session, WorkType workType)
        {

			Work work = new Work(entity, id, workType);
			worker.PerformWork(work,(ISessionImplementor) session);
            
        }

        public void SetbackendQueueProcessorFactory(IBackendQueueProcessorFactory backendQueueProcessorFactory)
        {
            this.backendQueueProcessorFactory = backendQueueProcessorFactory;
        }

        public void RegisterDirectoryProviderForLocks(IDirectoryProvider provider)
        {
            if (lockableDirectoryProviders.ContainsKey(provider) == false)
            {
                lockableDirectoryProviders.Add(provider, new object());
            }
        }
    }
}