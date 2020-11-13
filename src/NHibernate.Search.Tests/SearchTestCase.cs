using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using NHibernate.Cfg;
using NHibernate.Event;
using NHibernate.Impl;
using NHibernate.Search.Event;
using NHibernate.Search.Store;
using TestCase=NHibernate.Test.TestCase;

namespace NHibernate.Search.Tests
{
    public abstract class SearchTestCase : TestCase
    {
        protected override string MappingsAssembly
        {
            get { return "NHibernate.Search.Tests"; }
        }

        public static void SetListener(Configuration configure)
        {
            configure.SetListener(ListenerType.PostUpdate, new FullTextIndexEventListener());
            configure.SetListener(ListenerType.PostInsert, new FullTextIndexEventListener());
            configure.SetListener(ListenerType.PostDelete, new FullTextIndexEventListener());
            configure.SetListener(ListenerType.PostCollectionRecreate, new FullTextIndexCollectionEventListener());
            configure.SetListener(ListenerType.PostCollectionRemove, new FullTextIndexCollectionEventListener());
            configure.SetListener(ListenerType.PostCollectionUpdate, new FullTextIndexCollectionEventListener());
        }

        protected Lucene.Net.Store.Directory GetDirectory(System.Type clazz)
        {
            return GetLuceneEventListener().SearchFactory.GetDirectoryProviders(clazz)[0].Directory;
        }

        protected override void Configure(Configuration configuration)
        {
            cfg.SetProperty("hibernate.search.default.directory_provider", typeof(RAMDirectoryProvider).AssemblyQualifiedName);
            cfg.SetProperty(Environment.AnalyzerClass, typeof(StopAnalyzer).AssemblyQualifiedName);
            cfg.SetProperty("hibernate.search.default.transaction.merge_factor", "100");
            cfg.SetProperty("hibernate.search.default.batch.max_buffered_docs", "1000");
            SetListener(cfg);
        }

        private FullTextIndexEventListener GetLuceneEventListener()
        {
            IPostInsertEventListener[] listeners = ((SessionFactoryImpl)sessions).EventListeners.PostInsertEventListeners;
            FullTextIndexEventListener listener = null;

            // HACK: this sucks since we mandate the event listener use
            foreach (IPostInsertEventListener candidate in listeners)
            {
                if (typeof(FullTextIndexEventListener).IsAssignableFrom(candidate.GetType()))
                {
                    listener = (FullTextIndexEventListener)candidate;
                    break;
                }
            }

            if (listener == null)
            {
                throw new HibernateException("Lucene event listener not initialized");
            }

            return listener;
        }
    }
}