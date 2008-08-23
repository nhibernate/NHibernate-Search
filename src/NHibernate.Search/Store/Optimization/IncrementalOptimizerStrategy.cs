using System;
using log4net;
using Lucene.Net.Index;

namespace NHibernate.Search.Store.Optimization
{
    /// <summary>
    /// Optimization strategy triggered after a certain amount of operations
    /// </summary>
    public class IncrementalOptimizerStrategy : IOptimizerStrategy
    {
    	private int operationMax = -1;
        private int transactionMax = -1;
        private long operations = 0;
        private long transactions = 0;
        private IDirectoryProvider directoryProvider;
        private static readonly ILog log = LogManager.GetLogger(typeof(IncrementalOptimizerStrategy));

        public void Initialize(IDirectoryProvider directoryProvider, System.Collections.IDictionary indexProperties, NHibernate.Search.Engine.ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.directoryProvider = directoryProvider;
            string maxString = (string) indexProperties["optimizer.operation_limit.max"];
            if (!string.IsNullOrEmpty(maxString))
                int.TryParse(maxString, out operationMax);

            maxString = indexProperties["optimizer.transaction_limit.max"].ToString();
            if (!string.IsNullOrEmpty(maxString))
                int.TryParse(maxString, out transactionMax);
        }

        public void OptimizationForced()
        {
            operations = 0;
            transactions = 0;
        }

        public bool NeedOptimization()
        {
            return (operationMax != -1 && operations >= operationMax) || (transactionMax != -1 && transactions >= transactionMax);
        }

        public void AddTransaction(long operations)
        {
            this.operations += operations;
            transactions++;
        }

        public void Optimize(NHibernate.Search.Backend.Workspace workspace)
        {
            if (NeedOptimization())
            {
                IndexWriter writer = workspace.GetIndexWriter(directoryProvider);
                try
                {
                    writer.Optimize();
                }
                catch (Exception e)
                {
                }
                OptimizationForced();
            }
        }
    }
}
