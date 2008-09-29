using System.Collections;
using NHibernate.Engine;
using NHibernate.Search.Impl;

namespace NHibernate.Search.Backend
{
    /// <summary>
    /// Perform work for a given session. This implementation has to be multi threaded
    /// </summary>
    public interface IWorker
    {
        /// <summary>
        /// Perform the work on the session
        /// </summary>
        /// <param name="work"></param>
        /// <param name="session"></param>
        void PerformWork(Work work, ISessionImplementor session);

        /// <summary>
        /// Initialize the worker
        /// </summary>
        /// <param name="props"></param>
        /// <param name="searchFactory"></param>
        void Initialize(IDictionary props, SearchFactoryImpl searchFactory);
    }
}