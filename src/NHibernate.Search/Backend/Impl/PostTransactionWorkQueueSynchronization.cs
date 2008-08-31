using NHibernate.Transaction;
using NHibernate.Util;

namespace NHibernate.Search.Backend.Impl
{
    internal class PostTransactionWorkQueueSynchronization : ISynchronization
    {
        private bool consumed;
        private WorkQueue queue = new WorkQueue();
        private IQueueingProcessor queueingProcessor;
        private WeakHashtable queuePerTransaction;
        /**
		 * in transaction work
		 */

        public PostTransactionWorkQueueSynchronization(IQueueingProcessor queueingProcessor,
                                                       WeakHashtable queuePerTransaction)
        {
            this.queueingProcessor = queueingProcessor;
            this.queuePerTransaction = queuePerTransaction;
        }

        #region ISynchronization Members

        public void BeforeCompletion()
        {
            queueingProcessor.PrepareWorks(queue);
        }

        public void AfterCompletion(bool success)
        {
            try
            {
                if (success)
                    queueingProcessor.PerformWorks(queue);
                else
                    queueingProcessor.CancelWorks(queue);
            }
            finally
            {
                consumed = true;
                //clean the Synchronization per Transaction
                //not needed stricto sensus but a cleaner approach and faster than the GC
                if (queuePerTransaction != null) queuePerTransaction.Remove(this);
            }
        }

        #endregion

        public void add(Work work)
        {
            queueingProcessor.Add(work, queue);
        }

        public bool isConsumed()
        {
            return consumed;
        }
    }
}