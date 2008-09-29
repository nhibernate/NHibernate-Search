using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NHibernate.Search.Impl;

namespace NHibernate.Search.Backend.Impl.Lucene
{
    public class LuceneBackendQueueProcessorFactory : IBackendQueueProcessorFactory
    {
        private SearchFactoryImpl searchFactory;

        #region IBackendQueueProcessorFactory Members

        public void Initialize(IDictionary props, SearchFactoryImpl aSearchFactory)
        {
            this.searchFactory = aSearchFactory;
        }

        public WaitCallback GetProcessor(IList<LuceneWork> queue)
        {
            return new LuceneBackendQueueProcessor(queue, searchFactory).Run;
        }

        #endregion
    }
}