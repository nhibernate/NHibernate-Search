﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using Lucene.Net.Analysis.Core;
using NHibernate.Cfg;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Worker
{
    using System.Threading.Tasks;
    [TestFixture]
    public class SyncWorkerTestAsync : WorkerTestCaseAsync
    {
        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            configuration.SetProperty("hibernate.search.default.directory_provider", typeof(Store.RAMDirectoryProvider).AssemblyQualifiedName);
            configuration.SetProperty(Environment.AnalyzerClass, typeof(StopAnalyzer).AssemblyQualifiedName);
            configuration.SetProperty(Environment.WorkerScope, "transaction");
            configuration.SetProperty(Environment.WorkerExecution, "sync"); // Note: It is WorkerPrefix in the Java version, but it must be a typo
        }
    }
}