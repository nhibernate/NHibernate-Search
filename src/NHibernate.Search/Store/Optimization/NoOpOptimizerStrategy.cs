using System.Collections.Generic;
using NHibernate.Search.Backend;
using NHibernate.Search.Engine;

namespace NHibernate.Search.Store.Optimization
{
    public class NoOpOptimizerStrategy : IOptimizerStrategy
    {
        public void Initialize(IDirectoryProvider directoryProvider, IDictionary<string, string> indexProperties, ISearchFactoryImplementor searchFactoryImplementor)
        {
        }

        public void OptimizationForced()
        {
        }

        public bool NeedOptimization()
        {
            return false;
        }

        public void AddTransaction(long operations)
        {
        }

        public void Optimize(Workspace workspace)
        {
        }
    }
}