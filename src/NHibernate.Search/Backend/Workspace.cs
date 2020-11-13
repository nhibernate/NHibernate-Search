using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Search.Store;
using NHibernate.Search.Store.Optimization;

namespace NHibernate.Search.Backend
{
    /// <summary>
    /// Lucene workspace
    /// This is not intended to be used in a multithreaded environment
    /// <p/>
    /// One cannot execute modification through an IndexReader when an IndexWriter has been acquired on the same underlying directory
    /// One cannot get an IndexWriter when an IndexReader have been acquired and modified the same underlying directory
    /// The recommended approach is to execute all the modifications on the IndexReaders, {@link #Dispose()} }, and acquire the
    /// index writers
    /// </summary>
    /// TODO introduce the notion of read only IndexReader? We cannot enforce it because Lucene use abstract classes, not interfaces
    public class Workspace : IDisposable
    {
		private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(Workspace));

        private readonly Dictionary<IDirectoryProvider, IndexReader> readers;
        private readonly Dictionary<IDirectoryProvider, IndexWriter> writers;
        private readonly List<IDirectoryProvider> lockedProviders;
        private readonly Dictionary<IDirectoryProvider, DPStatistics> dpStatistics;
        private readonly ISearchFactoryImplementor searchFactoryImplementor;
        private bool isBatch;

        #region Constructors

        public Workspace(ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.readers = new Dictionary<IDirectoryProvider, IndexReader>();
            this.writers = new Dictionary<IDirectoryProvider, IndexWriter>();
            this.lockedProviders = new List<IDirectoryProvider>();
            this.dpStatistics = new Dictionary<IDirectoryProvider, DPStatistics>();
            this.searchFactoryImplementor = searchFactoryImplementor;
        }

        #endregion

        #region Property methods

        /// <summary>
        /// Flag indicating if the current work should be executed the Lucene parameters for batch indexing.
        /// </summary>
        public bool IsBatch
        {
            get { return isBatch; }
            set { isBatch = value; }
        }

        #endregion

        #region Public methods

        #region IDisposable Members

        /// <summary>
        /// release resources consumed in the workspace if any
        /// </summary>
        public void Dispose()
        {
            CleanUp(null);
        }

        #endregion

        public DocumentBuilder GetDocumentBuilder(System.Type entity)
        {
            DocumentBuilder builder;
            searchFactoryImplementor.DocumentBuilders.TryGetValue(entity, out builder);
            return builder;
        }

        public IndexReader GetIndexReader(IDirectoryProvider provider, System.Type entity)
        {
            // one cannot access a reader for update after a writer has been accessed
            if (writers.ContainsKey(provider))
            {
                throw new AssertionFailure("Tries to read for update a index while a writer is accessed" + entity);
            }

            IndexReader reader;
            readers.TryGetValue(provider, out reader);
            if (reader != null)
            {
                return reader;
            }

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
                    reader.Dispose();
                }
                catch (IOException ex)
                {
                    throw new SearchException("Exception while closing IndexReader", ex);
                }
                finally
                {
                    readers.Remove(provider);

                    // PH - Moved the exit lock out of the try otherwise it won't take place when we have an error closing the reader.
                    // Exit Lock added by Kailuo Wang, because the lock needs to be obtained immediately afterwards
                    var syncLock = searchFactoryImplementor.GetLockableDirectoryProviders()[provider];
                    Monitor.Exit(syncLock);
                }
            }

            if (writers.ContainsKey(provider))
            {
                return writers[provider];
            }

            LockProvider(provider);

            if (modificationOperation)
            {
                dpStatistics[provider].Operations++;
            }

            try
            {
                var analyzer = entity != null
                                        ? searchFactoryImplementor.DocumentBuilders[entity].Analyzer
                                        : new StandardAnalyzer(LuceneVersion.LUCENE_48);
                var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
                var indexingParams = searchFactoryImplementor.GetIndexingParameters(provider);
                if (IsBatch)
                {
                    indexingParams.BatchIndexParameters.ApplyToWriterConfig(config);
                }
                else
                {
                    indexingParams.TransactionIndexParameters.ApplyToWriterConfig(config);
                }

                var writer = new IndexWriter(provider.Directory, config);

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

        #region Private methods

        private void LockProvider(IDirectoryProvider provider)
        {
            // Make sure to use a semaphore
            object syncLock = searchFactoryImplementor.GetLockableDirectoryProviders()[provider];
            Monitor.Enter(syncLock);
            try
            {
                if (!lockedProviders.Contains(provider))
                {
                    lockedProviders.Add(provider);
                    dpStatistics.Add(provider, new DPStatistics());
                }
            }
            catch (Exception)
            {
                // NB This is correct here, we release the lock *only* if we have an error
                Monitor.Exit(syncLock);
                throw;
            }
        }

        private void CleanUp(SearchException originalException)
        {
            // Release all readers and writers, then release locks
            SearchException raisedException = originalException;
            foreach (IndexReader reader in readers.Values)
            {
                try
                {
                    reader.Dispose();
                }
                catch (IOException e)
                {
                    if (raisedException != null)
                    {
                        log.Error("Subsequent Exception while closing IndexReader", e);
                    }
                    else
                    {
                        raisedException = new SearchException("Exception while closing IndexReader", e);
                    }
                }
            }
            readers.Clear();

            // TODO release lock of all indexes that do not need optimization early
            // don't optimize if there is a failure
            if (raisedException == null)
            {
                foreach (IDirectoryProvider provider in lockedProviders)
                {
                    var stats = dpStatistics[provider];
                    if (!stats.OptimizationForced)
                    {
                        IOptimizerStrategy optimizerStrategy = searchFactoryImplementor.GetOptimizerStrategy(provider);
                        optimizerStrategy.AddTransaction(stats.Operations);
                        try
                        {
                            optimizerStrategy.Optimize(this);
                        }
                        catch (SearchException e)
                        {
                            raisedException = new SearchException("Exception whilst optimizing directoryProvider: " + provider.Directory, e);
                            break; // No point in continuing
                        }
                    }
                }
            }

            foreach (IndexWriter writer in writers.Values)
            {
                try
                {
                    writer.Dispose();
                }
                catch (IOException e)
                {
                    if (raisedException != null)
                    {
                        log.Error("Subsequent Exception while closing IndexWriter", e);
                    }
                    else
                    {
                        raisedException = new SearchException("Exception while closing IndexWriter", e);
                    }
                }
            }

            foreach (IDirectoryProvider provider in lockedProviders)
            {
                object syncLock = searchFactoryImplementor.GetLockableDirectoryProviders()[provider];
                Monitor.Exit(syncLock);
            }

            writers.Clear();
            lockedProviders.Clear();
            dpStatistics.Clear();

            if (raisedException != null)
            {
                throw raisedException;
            }
        }

        #endregion

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
    }
}