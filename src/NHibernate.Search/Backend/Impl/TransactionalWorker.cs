using System.Collections;
using NHibernate.Engine;
using NHibernate.Search.Impl;
using NHibernate.Util;

namespace NHibernate.Search.Backend.Impl
{
    public class TransactionalWorker : IWorker
    {
        //not a synchronized map since for a given transaction, we have not concurrent access
        private IQueueingProcessor queueingProcessor;
        protected WeakHashtable synchronizationPerTransaction = new WeakHashtable();

        #region IWorker Members

        public void PerformWork(Work work, ISessionImplementor session)
        {
            if (session.TransactionInProgress)
            {
                ITransaction transaction = ((ISession)session).Transaction;
                PostTransactionWorkQueueSynchronization txSync = (PostTransactionWorkQueueSynchronization)
                                                                 synchronizationPerTransaction[transaction];
                if (txSync == null || txSync.IsConsumed)
                {
                    txSync =
                        new PostTransactionWorkQueueSynchronization(queueingProcessor, synchronizationPerTransaction);
                    transaction.RegisterSynchronization(txSync);
                    lock (synchronizationPerTransaction.SyncRoot)
                        synchronizationPerTransaction[transaction] = txSync;
                }
                txSync.Add(work);
            }
            else
            {
                WorkQueue queue = new WorkQueue(2); //one work can be split
                queueingProcessor.Add(work, queue);
                queueingProcessor.PrepareWorks(queue);
                queueingProcessor.PerformWorks(queue);
            }
        }

        #endregion

        public void Initialize(IDictionary props, SearchFactoryImpl searchFactory)
        {
            queueingProcessor = new BatchedQueueingProcessor(searchFactory, props);
        }
    }
}