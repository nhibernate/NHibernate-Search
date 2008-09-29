using System;
using System.Collections.Generic;
using log4net;
using Lucene.Net.Index;
using NHibernate.Search.Backend;
using NHibernate.Search.Engine;

namespace NHibernate.Search.Store.Optimization
{
    /// <summary>
    /// Optimization strategy triggered after a certain amount of operations
    /// </summary>
    public class IncrementalOptimizerStrategy : IOptimizerStrategy
    {
        private int operationMax = -1;
        private int transactionMax = -1;
        private long operations;
        private long transactions;
        private IDirectoryProvider directoryProvider;
        private static readonly ILog log = LogManager.GetLogger(typeof(IncrementalOptimizerStrategy));

        public void Initialize(IDirectoryProvider directoryProvider, IDictionary<string, string> indexProperties,
                               ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.directoryProvider = directoryProvider;
            string maxString;
            indexProperties.TryGetValue("optimizer.operation_limit.max", out maxString);

            if (!string.IsNullOrEmpty(maxString))
                int.TryParse(maxString, out operationMax);

            indexProperties.TryGetValue("optimizer.transaction_limit.max", out maxString);
            if (!string.IsNullOrEmpty(maxString))
                int.TryParse(maxString, out transactionMax);
        }

        public void OptimizationForced()
        {
            operations = 0;
            transactions = 0;
        }

        public bool NeedOptimization
        {
            get
            {
                return (operationMax != -1 && operations >= operationMax) ||
                       (transactionMax != -1 && transactions >= transactionMax);
            }
        }

        public void AddTransaction(long theOperations)
        {
            this.operations += theOperations;
            transactions++;
        }

        public void Optimize(Workspace workspace)
        {
            if (!NeedOptimization) 
                return;
            IndexWriter writer = workspace.GetIndexWriter(directoryProvider);
            try
            {
                writer.Optimize();
            }
            catch (Exception)
            {
            }
            OptimizationForced();
        }
    }
}