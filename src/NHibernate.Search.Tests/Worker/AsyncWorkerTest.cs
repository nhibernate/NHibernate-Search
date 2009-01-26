using NHibernate.Cfg;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Worker
{
    [TestFixture]
    public class AsyncWorkerTest : WorkerTestCase
    {
        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            configuration.SetProperty("hibernate.search.default.directory_provider", typeof(Store.RAMDirectoryProvider).AssemblyQualifiedName);
            configuration.SetProperty(Environment.AnalyzerClass, typeof(Lucene.Net.Analysis.StopAnalyzer).AssemblyQualifiedName);
            configuration.SetProperty(Environment.WorkerScope, "transaction");
            configuration.SetProperty(Environment.WorkerExecution, "async");
            configuration.SetProperty(Environment.WorkerPrefix + "thread_pool.size", "1");
            configuration.SetProperty(Environment.WorkerPrefix + "buffer_queue.max", "10");
        }
    }
}