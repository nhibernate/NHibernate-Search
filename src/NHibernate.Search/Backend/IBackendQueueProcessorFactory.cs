using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace NHibernate.Search.Backend
{
    using Engine;

    /// <summary>
    /// Build stateful backend processor
    /// Must have a no arg constructor
    /// The factory typically prepare or pool the resources needed by the queue processor
    /// </summary>
    public interface IBackendQueueProcessorFactory
    {
        void Initialize(IDictionary props, ISearchFactoryImplementor aSearchFactory);

        /// <summary>
        /// Return a runnable implementation responsible for processing the queue to a given backend
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        WaitCallback GetProcessor(IList<LuceneWork> queue);
    }
}