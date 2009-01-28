using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace NHibernate.Search.Backend.Impl.Lucene
{
    using Engine;

    public class LuceneBackendQueueProcessorFactory : IBackendQueueProcessorFactory
    {
        private ISearchFactoryImplementor searchFactoryImplementor;

        #region IBackendQueueProcessorFactory Members

        public void Initialize(IDictionary props, ISearchFactoryImplementor aSearchFactoryImplementor)
        {
            this.searchFactoryImplementor = aSearchFactoryImplementor;
        }

        public WaitCallback GetProcessor(IList<LuceneWork> queue)
        {
            return new LuceneBackendQueueProcessor(queue, searchFactoryImplementor).Run;
        }

        #endregion
    }
}