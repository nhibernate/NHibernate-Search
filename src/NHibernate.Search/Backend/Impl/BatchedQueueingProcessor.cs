using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NHibernate.Search.Backend.Impl.Lucene;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Util;

namespace NHibernate.Search.Backend.Impl
{
    /// <summary>
    ///  Batch work until <c>ExecuteQueue</c> is called.
    ///  The work is then executed synchronously or asynchronously
    /// </summary>
    public class BatchedQueueingProcessor : IQueueingProcessor
    {
        private readonly bool sync;
        private readonly int batchSize;
        private readonly IBackendQueueProcessorFactory backendQueueProcessorFactory;
        private readonly ISearchFactoryImplementor searchFactoryImplementor;

        public BatchedQueueingProcessor(ISearchFactoryImplementor searchFactoryImplementor, IDictionary properties)
        {
            this.searchFactoryImplementor = searchFactoryImplementor;
            //default to sync if none defined
            this.sync = !"async".Equals((string)properties[Environment.WorkerExecution], StringComparison.InvariantCultureIgnoreCase);

            string backend = (string)properties[Environment.WorkerBackend];
            batchSize = 0;//(int) properties[Environment.WorkerBatchSize];
            if (StringHelper.IsEmpty(backend) || "lucene".Equals(backend, StringComparison.InvariantCultureIgnoreCase))
            {
                backendQueueProcessorFactory = new LuceneBackendQueueProcessorFactory();
            }
            else
            {
                try
                {
                    System.Type processorFactoryClass = ReflectHelper.ClassForName(backend);
                    backendQueueProcessorFactory = (IBackendQueueProcessorFactory)Activator.CreateInstance(processorFactoryClass);
                }
                catch (Exception e)
                {
                    throw new SearchException("Unable to find/create processor class: " + backend, e);
                }
            }
            backendQueueProcessorFactory.Initialize(properties, searchFactoryImplementor);
            searchFactoryImplementor.BackendQueueProcessorFactory = backendQueueProcessorFactory;
        }

        #region IQueueingProcessor Members

        public void Add(Work work, WorkQueue workQueue)
        {
            //don't check for builder it's done in prepareWork
            //FIXME WorkType.COLLECTION does not play well with batchSize
            workQueue.Add(work);
            if (batchSize > 0 && workQueue.Count >= batchSize)
            {
                WorkQueue subQueue = workQueue.SplitQueue();
                PrepareWorks(subQueue);
                PerformWorks(subQueue);
            }
        }

        //TODO implements parallel batchWorkers (one per Directory)
        public void PerformWorks(WorkQueue workQueue)
        {
            WaitCallback processor = backendQueueProcessorFactory.GetProcessor(workQueue.GetSealedQueue());
            if (sync)
                processor(null);
            else
                ThreadPool.QueueUserWorkItem(processor);
        }

        public void CancelWorks(WorkQueue workQueue)
        {
            workQueue.Clear();
        }

        public void PrepareWorks(WorkQueue workQueue)
        {
            List<Work> queue = workQueue.GetQueue();
            int initialSize = queue.Count;
            List<LuceneWork> luceneQueue = new List<LuceneWork>(initialSize); //TODO load factor for containedIn
            /**
			 * Collection work type are processed second, so if the owner entity has already been processed for whatever reason
			 * the work will be ignored.
			 * However if the owner entity has not been processed, an "UPDATE" work is executed
			 *
			 * Processing collection works last is mandatory to avoid reindexing a object to be deleted
			 */
            ProcessWorkByLayer(queue, initialSize, luceneQueue, Layer.FIRST);
            ProcessWorkByLayer(queue, initialSize, luceneQueue, Layer.SECOND);
            workQueue.SetSealedQueue(luceneQueue);
        }

        #endregion

        private void ProcessWorkByLayer(IList<Work> queue, int initialSize, List<LuceneWork> luceneQueue, Layer layer)
        {
            /* By Kailuo Wang
             * This sequence of the queue is reversed which is different from the Java version
             * By reversing the sequence here, it ensures that the work that is added to the queue later has higher priority.
             * I did this to solve the following problem I encountered:
             * If you update an entity before deleting it in the same transaction,
             * There will be two Works generated by the event listener: Update Work and Delete Work.
             * However, the Update Work will prevent the Delete Work from being added to the queue and thus 
             * fail purging the index for that entity. 
             * I am not sure if the Java version has the same problem.
             */
            for (int i = initialSize - 1; i >= 0; i--)
            {
                Work work = queue[i];
                if (work == null || !layer.IsRightLayer(work.WorkType))
                {
                    continue;
                }

                queue[i] = null; // help GC and avoid 2 loaded queues in memory
                System.Type entityClass = work.Entity is System.Type
                                          ? (System.Type)work.Entity
                                          : NHibernateUtil.GetClass(work.Entity);

                DocumentBuilder builder = this.searchFactoryImplementor.DocumentBuilders[entityClass];
                if (builder == null)
                {
                    continue; //or exception?
                }

                builder.AddToWorkQueue(entityClass, work.Entity, work.Id, work.WorkType, luceneQueue, this.searchFactoryImplementor);
            }
        }

        #region Nested type: Layer

        private abstract class Layer
        {
            public static readonly Layer FIRST = new First();
            public static readonly Layer SECOND = new Second();
            public abstract bool IsRightLayer(WorkType type);

            #region Nested type: First

            private class First : Layer
            {
                public override bool IsRightLayer(WorkType type)
                {
                    return type != WorkType.Collection;
                }
            }

            #endregion

            #region Nested type: Second

            private class Second : Layer
            {
                public override bool IsRightLayer(WorkType type)
                {
                    return type == WorkType.Collection;
                }
            }

            #endregion
        }

        #endregion
    }
}
