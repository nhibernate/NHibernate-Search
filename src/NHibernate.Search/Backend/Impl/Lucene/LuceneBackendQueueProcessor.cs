using System.Collections.Generic;
using NHibernate.Search.Engine;
using NHibernate.Search.Store;

namespace NHibernate.Search.Backend.Impl.Lucene
{
    /// <summary>
    /// Apply the operations to Lucene directories avoiding deadlocks
    /// </summary>
    public class LuceneBackendQueueProcessor
    {
        private readonly IList<LuceneWork> queue;
        private readonly ISearchFactoryImplementor searchFactoryImplementor;

        public LuceneBackendQueueProcessor(IList<LuceneWork> queue, ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.queue = queue;
            this.searchFactoryImplementor = searchFactoryImplementor;
        }

        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignore">Ignored, used to keep the delegate signature that WaitCallback requires</param>
        public void Run(object ignore)
        {
            Workspace workspace = new Workspace(searchFactoryImplementor);
            LuceneWorker worker = new LuceneWorker(workspace);
            try
            {
                List<LuceneWorker.WorkWithPayload> queueWithFlatDPs = new List<LuceneWorker.WorkWithPayload>(queue.Count*2);
                foreach (LuceneWork work in queue)
                {
                    DocumentBuilder documentBuilder = searchFactoryImplementor.DocumentBuilders[work.EntityClass];
                    IIndexShardingStrategy shardingStrategy = documentBuilder.DirectoryProvidersSelectionStrategy;
                    if (work is PurgeAllLuceneWork)
                    {
                        IDirectoryProvider[] providers = shardingStrategy.GetDirectoryProvidersForDeletion(work.EntityClass, work.Id, work.IdInString);
                        foreach (IDirectoryProvider provider in providers)
                        {
                            queueWithFlatDPs.Add(new LuceneWorker.WorkWithPayload(work, provider));
                        }
                    }
                    else if (work is AddLuceneWork)
                    {
                        IDirectoryProvider provider = shardingStrategy.GetDirectoryProviderForAddition(work.EntityClass, work.Id, work.IdInString, work.Document);
                        queueWithFlatDPs.Add(new LuceneWorker.WorkWithPayload(work, provider));
                    }
                    else if (work is DeleteLuceneWork)
                    {
                        IDirectoryProvider[] providers = shardingStrategy.GetDirectoryProvidersForDeletion(work.EntityClass, work.Id, work.IdInString);
                        foreach (IDirectoryProvider provider in providers)
                        {
                            queueWithFlatDPs.Add(new LuceneWorker.WorkWithPayload(work, provider));
                        }
                    }
                    else if (work is OptimizeLuceneWork)
                    {
                        IDirectoryProvider[] providers = shardingStrategy.GetDirectoryProvidersForAllShards();
                        foreach (IDirectoryProvider provider in providers)
                        {
                            queueWithFlatDPs.Add(new LuceneWorker.WorkWithPayload(work, provider));
                        }
                    }
                    else
                    {
                        throw new AssertionFailure("Unknown work type: " + work.GetType());
                    }
                }

                DeadLockFreeQueue(queueWithFlatDPs, searchFactoryImplementor);
                CheckForBatchIndexing(workspace);
                foreach (LuceneWorker.WorkWithPayload luceneWork in queueWithFlatDPs)
                {
                    worker.PerformWork(luceneWork);
                }
            }
            finally
            {
                workspace.Dispose();
                queue.Clear();
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// one must lock the directory providers in the exact same order to avoid
        /// dead lock between concurrent threads or processes
        /// To achieve that, the work will be done per directory provider
        /// We rely on the both the DocumentBuilder.GetHashCode() and the GetWorkHashCode() to 
        /// sort them by predictive order at all times, and to put deletes before adds
        /// </summary>
        private static void DeadLockFreeQueue(List<LuceneWorker.WorkWithPayload> queue,
                                              ISearchFactoryImplementor searchFactoryImplementor)
        {
            queue.Sort(delegate(LuceneWorker.WorkWithPayload x, LuceneWorker.WorkWithPayload y)
            {
                long h1 = GetWorkHashCode(x, searchFactoryImplementor);
                long h2 = GetWorkHashCode(y, searchFactoryImplementor);
                return h1 < h2 ? -1 : h1 == h2 ? 0 : 1;
            });
        }

        private static long GetWorkHashCode(LuceneWorker.WorkWithPayload luceneWork,
                                            ISearchFactoryImplementor searchFactoryImplementor)
        {
            IDirectoryProvider provider = luceneWork.Provider;
            int h = provider.GetHashCode();
            h = 31 * h + provider.GetHashCode();
            long extendedHash = h; //to be sure extendedHash + 1 < extendedHash + 2 is always true
            if (luceneWork.Work is AddLuceneWork)
            {
                extendedHash += 1; //addwork after deleteWork
            }

            if (luceneWork.Work is OptimizeLuceneWork)
            {
                extendedHash += 2; //optimize after everything
            }

            return extendedHash;
        }

        private void CheckForBatchIndexing(Workspace workspace)
        {
            foreach (LuceneWork luceneWork in queue)
            {
                // if there is at least a single batch index job we put the work space into batch indexing mode.
                if (!luceneWork.IsBatch)
                {
                    continue;
                }

                workspace.IsBatch = true;
                break;
            }
        }

        #endregion
    }
}