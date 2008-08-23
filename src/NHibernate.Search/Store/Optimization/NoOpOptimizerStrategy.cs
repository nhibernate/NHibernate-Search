using System;
using System.Collections.Generic;
using System.Text;

namespace NHibernate.Search.Store.Optimization
{
    public class NoOpOptimizerStrategy : IOptimizerStrategy
    {
        public void Initialize(IDirectoryProvider directoryProvider, System.Collections.IDictionary indexProperties, NHibernate.Search.Engine.ISearchFactoryImplementor searchFactoryImplementor)
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

        public void Optimize(NHibernate.Search.Backend.Workspace workspace)
        {
        }
    }
}
