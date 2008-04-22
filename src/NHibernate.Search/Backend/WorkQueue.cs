using System.Collections.Generic;

namespace NHibernate.Search.Backend {
	/**
	 * @author Emmanuel Bernard
	 */

	public class WorkQueue {
		//TODO set a serial number
		private List<Work> queue;

		private List<LuceneWork> sealedQueue;

		public WorkQueue(int size) {
			queue = new List<Work>(size);
		}

		private WorkQueue(List<Work> queue) {
			this.queue = queue;
		}

		public WorkQueue() : this(10) {}

		public void add(Work work) {
			queue.Add(work);
		}

		public List<Work> getQueue() {
			return queue;
		}

		public WorkQueue splitQueue() {
			WorkQueue subQueue = new WorkQueue(queue);
			queue = new List<Work>(queue.Count);
			return subQueue;
		}

		public List<LuceneWork> getSealedQueue() {
			if (sealedQueue == null) throw new AssertionFailure("Access a Sealed WorkQueue whcih has not been sealed");
			return sealedQueue;
		}

		public void setSealedQueue(List<LuceneWork> sealedQueue) {
			//invalidate the working queue for serializability
			queue = null;
			this.sealedQueue = sealedQueue;
		}

		public void clear() {
			queue.Clear();
			if (sealedQueue != null) sealedQueue.Clear();
		}

		public int size() {
			return queue.Count;
		}
	}
}