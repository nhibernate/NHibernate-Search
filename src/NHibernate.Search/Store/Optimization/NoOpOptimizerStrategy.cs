using System.Collections.Generic;
using NHibernate.Search.Backend;
using NHibernate.Search.Engine;

namespace NHibernate.Search.Store.Optimization
{
    public class NoOpOptimizerStrategy : IOptimizerStrategy
    {
        public bool NeedOptimization
        {
            get { return false; }
        }

        public void Initialize(IDirectoryProvider directoryProvider, IDictionary<string, string> indexProperties, ISearchFactoryImplementor searchFactoryImplementor)
        {
        }

        public void OptimizationForced()
        {
        }

        public void AddTransaction(long theOperations)
        {
        }

        public void Optimize(Workspace workspace)
        {
        }
    }
}