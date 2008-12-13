using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using log4net;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Search.Store;
using NHibernate.Search.Store.Optimization;

namespace NHibernate.Search.Backend
{
    //TODO introduce the notion of read only IndexReader? We cannot enforce it because Lucene use abstract classes, not interfaces
    /// <summary>
    /// Lucene workspace
    /// This is not intended to be used in a multithreaded environment
    /// <p/>
    /// One cannot execute modification through an IndexReader when an IndexWriter has been acquired on the same underlying directory
    /// One cannot get an IndexWriter when an IndexReader have been acquired and modified the same underlying directory
    /// The recommended approach is to execute all the modifications on the IndexReaders, {@link #Dispose()} }, and acquire the
    /// index writers
    /// </summary>
    public class Workspace : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Workspace));

        private readonly Dictionary<IDirectoryProvider, IndexReader> readers = new Dictionary<IDirectoryProvider, IndexReader>();
        private readonly Dictionary<IDirectoryProvider, IndexWriter> writers = new Dictionary<IDirectoryProvider, IndexWriter>();
        private readonly List<IDirectoryProvider> lockedProviders = new List<IDirectoryProvider>();
        private readonly Dictionary<IDirectoryProvider, DPStatistics> dpStatistics = new Dictionary<IDirectoryProvider, DPStatistics>();
        private readonly ISearchFactoryImplementor searchFactoryImplementor;
        private bool isBatch;

        #region Nested classes : DPStatistics

        private class DPStatistics
        {
            private bool optimizationForced;
            private long operations;

            /// <summary>
            /// 
            /// </summary>
            public bool OptimizationForced
            {
                get { return optimizationForced; }
                set { optimizationForced = value; }
            }

            /// <summary>
            /// 
            /// </summary>
            public long Operations
            {
                get { return operations; }
                set { operations = value; }
            }
        }

        #endregion

        #region Constructors

        public Workspace(ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.searchFactoryImplementor = searchFactoryImplementor;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// release resources consumed in the workspace if any
        /// </summary>
        public void Dispose()
        {
            CleanUp(null);
        }

        #endregion

        #region Property methods

        /// <summary>
        /// 
        /// </summary>
        public bool IsBatch
        {
            get { return isBatch; }
            set { isBatch = value; }
        }

        #endregion

        #region Private methods

        private void LockProvider(IDirectoryProvider provider)
        {
            //make sure to use a semaphore
            object syncLock = searchFactoryImplementor.GetLockableDirectoryProviders()[provider];
            Monitor.Enter(syncLock);
            if (!lockedProviders.Contains(provider))
            {
                lockedProviders.Add(provider);
                dpStatistics.Add(provider, new DPStatistics());
            }
        }

        private void CleanUp(SearchException originalException)
        {
            //release all readers and writers, then release locks
            SearchException raisedException = originalException;
            foreach (IndexReader reader in readers.Values)
            {
                try
                {
                    reader.Close();
                }
                catch (IOException e)
                {
                    if (raisedException != null)
                        log.Error("Subsequent Exception while closing IndexReader", e);
                    else
                        raisedException = new SearchException("Exception while closing IndexReader", e);
                }
            }
            readers.Clear();

            foreach (IndexWriter writer in writers.Values)
            {
                try
                {
                    writer.Close();
                }
                catch (IOException e)
                {
                    if (raisedException != null)
                        log.Error("Subsequent Exception while closing IndexWriter", e);
                    else
                        raisedException = new SearchException("Exception while closing IndexWriter", e);
                }
            }
            writers.Clear();

            foreach (IDirectoryProvider provider in lockedProviders)
            {
                object syncLock = searchFactoryImplementor.GetLockableDirectoryProviders()[provider];
                Monitor.Exit(syncLock);
            }
            lockedProviders.Clear();

            if (raisedException != null) throw raisedException;
        }

        #endregion

        #region Public methods

        public DocumentBuilder GetDocumentBuilder(System.Type entity)
        {
            DocumentBuilder builder;
            searchFactoryImplementor.DocumentBuilders.TryGetValue(entity, out builder);
            return builder;
        }

        public IndexReader GetIndexReader(IDirectoryProvider provider, System.Type entity)
        {
            //one cannot access a reader for update after a writer has been accessed
            if (writers.ContainsKey(provider))
                throw new AssertionFailure("Tries to read for update a index while a writer is accessed" + entity);
            IndexReader reader;
            readers.TryGetValue(provider, out reader);

            if (reader != null) return reader;
            LockProvider(provider);
            dpStatistics[provider].Operations++;
            try
            {
                reader = IndexReader.Open(provider.Directory);
                readers.Add(provider, reader);
            }
            catch (IOException e)
            {
                CleanUp(new SearchException("Unable to open IndexReader for " + entity, e));
            }

            return reader;
        }

        public IndexWriter GetIndexWriter(IDirectoryProvider provider)
        {
            return GetIndexWriter(provider, null, false);
        }

        /// <summary>
        /// Retrieve a read/write <see cref="IndexWriter" />
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="entity"></param>
        /// <param name="modificationOperation"></param>
        /// <returns></returns>
        public IndexWriter GetIndexWriter(IDirectoryProvider provider, System.Type entity, bool modificationOperation)
        {
            // Have to close the reader before the writer is accessed.
            IndexReader reader;
            readers.TryGetValue(provider, out reader);
            if (reader != null)
            {
                try
                {
                    reader.Close();
                    readers.Remove(provider);
                    //Exit Lock added by Kailuo Wang, because the lock needs to be obtained immediately afterwards
                    object syncLock = searchFactoryImplementor.GetLockableDirectoryProviders()[provider];
                    Monitor.Exit(syncLock);
                }
                catch (IOException ex)
                {
                    throw new SearchException("Exception while closing IndexReader", ex);
                }
               

            }

            if (writers.ContainsKey(provider))
                return writers[provider];

            LockProvider(provider);

            if (modificationOperation) dpStatistics[provider].Operations++;

            try
            {
                Analyzer analyzer = entity != null
                                        ? searchFactoryImplementor.DocumentBuilders[entity].Analyzer
                                        : new SimpleAnalyzer();
                IndexWriter writer = new IndexWriter(provider.Directory, analyzer, false);

                LuceneIndexingParameters indexingParams = searchFactoryImplementor.GetIndexingParameters(provider);
                if (IsBatch)
                {
                    writer.SetMergeFactor(indexingParams.BatchMergeFactor);
                    writer.SetMaxMergeDocs(indexingParams.BatchMaxMergeDocs);
                    writer.SetMaxBufferedDocs(indexingParams.BatchMaxBufferedDocs);
                }
                else
                {
                    writer.SetMergeFactor(indexingParams.TransactionMergeFactor);
                    writer.SetMaxMergeDocs(indexingParams.TransactionMaxMergeDocs);
                    writer.SetMaxBufferedDocs(indexingParams.TransactionMaxBufferedDocs);
                }

                writers.Add(provider, writer);

                return writer;
            }
            catch (IOException ex)
            {
                CleanUp(new SearchException("Unable to open IndexWriter" + (entity != null ? " for " + entity : ""), ex));
            }

            return null;
        }

 

        public void Optimize(IDirectoryProvider provider)
        {
            IOptimizerStrategy optimizerStrategy = searchFactoryImplementor.GetOptimizerStrategy(provider);
            dpStatistics[provider].OptimizationForced = true;
            optimizerStrategy.OptimizationForced();
        }

        #endregion
    }
}