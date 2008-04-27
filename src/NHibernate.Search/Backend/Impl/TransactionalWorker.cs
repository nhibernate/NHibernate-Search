using System.Collections;
using NHibernate.Engine;
using NHibernate.Search.Engine;
using NHibernate.Util;

namespace NHibernate.Search.Backend.Impl {
    public class TransactionalWorker : IWorker {
        //not a synchronized map since for a given transaction, we have not concurrent access
        private IQueueingProcessor queueingProcessor;
        protected WeakHashtable synchronizationPerTransaction = new WeakHashtable();

        #region IWorker Members

        public void PerformWork(Work work, ISessionImplementor session) {
            if (session.TransactionInProgress) {
                ITransaction transaction = session.GetSession().Transaction;
                PostTransactionWorkQueueSynchronization txSync = (PostTransactionWorkQueueSynchronization)
                                                                 synchronizationPerTransaction[transaction];
                if (txSync == null || txSync.isConsumed()) {
                    txSync =
                        new PostTransactionWorkQueueSynchronization(queueingProcessor, synchronizationPerTransaction);
                    transaction.RegisterSynchronization(txSync);
                    synchronizationPerTransaction[transaction] = txSync;
                }
                txSync.add(work);
            }
            else {
                WorkQueue queue = new WorkQueue(2); //one work can be split
                queueingProcessor.Add(work, queue);
                queueingProcessor.PrepareWorks(queue);
                queueingProcessor.PerformWorks(queue);
            }
        }

        public void Initialize(IDictionary props, SearchFactory searchFactory) {
            queueingProcessor = new BatchedQueueingProcessor(searchFactory, props);
        }

        #endregion
    }
}