namespace NHibernate.Search.Backend
{
    /// <summary>
    ///	 Pile work operations
    ///  No thread safety has to be implemented, the queue being thread scoped already
    ///  The implementation must be "stateless" wrt the queue through (ie not store the queue state)
    /// </summary>
    public interface IQueueingProcessor
    {
        void PerformWorks(WorkQueue workQueue);
        void CancelWorks(WorkQueue workQueue);
        void Add(Work work, WorkQueue workQueue);
        void PrepareWorks(WorkQueue workQueue);
    }
}