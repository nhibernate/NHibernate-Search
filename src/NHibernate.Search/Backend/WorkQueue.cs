using System.Collections.Generic;

namespace NHibernate.Search.Backend
{
    /// <summary>
    /// 
    /// </summary>
    public class WorkQueue
    {
        //TODO set a serial number
        private List<Work> queue;

        private List<LuceneWork> sealedQueue;

        public WorkQueue(int size)
        {
            queue = new List<Work>(size);
        }

        private WorkQueue(List<Work> queue)
        {
            this.queue = queue;
        }

        public WorkQueue() : this(10)
        {
        }

        public void Add(Work work)
        {
            queue.Add(work);
        }

        public List<Work> GetQueue()
        {
            return queue;
        }

        public WorkQueue SplitQueue()
        {
            WorkQueue subQueue = new WorkQueue(queue);
            queue = new List<Work>(queue.Count);
            return subQueue;
        }

        public List<LuceneWork> GetSealedQueue()
        {
            if (sealedQueue == null) 
                throw new AssertionFailure("Access a Sealed WorkQueue which has not been sealed");
            return sealedQueue;
        }

        public void SetSealedQueue(List<LuceneWork> sealedQueue)
        {
            //invalidate the working queue for serializability
            queue = null;
            this.sealedQueue = sealedQueue;
        }

        public void Clear()
        {
            queue.Clear();
            if (sealedQueue != null) sealedQueue.Clear();
        }

        public int Count
        {
            get { return queue.Count; }
        }
    }
}