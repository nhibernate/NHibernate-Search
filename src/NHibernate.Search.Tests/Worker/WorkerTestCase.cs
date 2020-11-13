using System;
using System.Collections;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NHibernate.Cfg;
using NHibernate.Search.Store;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Worker
{
    [TestFixture]
    public class WorkerTestCase : SearchTestCase
    {
        private volatile int worksCount;
        private volatile int reverseWorksCount;
        private volatile int errorsCount;

        protected override IList Mappings
        {
            get
            {
                return new string[]
                           {
                               "Worker.Employee.hbm.xml",
                               "Worker.Employer.hbm.xml",
                               "Worker.Food.hbm.xml",
                               "Worker.Drink.hbm.xml",
                           };
            }
        }

        #region Tests

        [Test]
        public void Concurrency()
        {
            const int nThreads = 15; // Fixed number of threads
            int workerThreads, completionPortThreads;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMinThreads(nThreads, completionPortThreads);
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMaxThreads(nThreads, completionPortThreads);
            int workerThreadsAvailable;
            ThreadPool.GetAvailableThreads(out workerThreadsAvailable, out completionPortThreads);

            long start = DateTime.Now.Ticks;
            const int iteration = 20; // Note: Was 100
            for (int i = 0; i < iteration; i++)
            {
                ThreadPool.QueueUserWorkItem(Work);
                ThreadPool.QueueUserWorkItem(ReverseWork);
            }

            do
            {
                ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
                Thread.Sleep(20); // Wait that all the threads have been terminated before, otherwise, they will be aborted
            }
            while (workerThreads != workerThreadsAvailable
                || worksCount < iteration || reverseWorksCount < iteration);

            System.Diagnostics.Debug.WriteLine(iteration + " iterations in " + nThreads
                + " threads: " + new TimeSpan(DateTime.Now.Ticks - start).TotalSeconds + " secs; errorsCount = " + errorsCount);
            Assert.AreEqual(0, errorsCount, "Some iterations failed");
        }

        #endregion

        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);

            configuration.SetProperty("hibernate.search.default.directory_provider", typeof(FSDirectoryProvider).AssemblyQualifiedName);
            configuration.SetProperty(Environment.AnalyzerClass, typeof(StopAnalyzer).AssemblyQualifiedName);
        }

        private void Work(object state)
        {
            Employee ee = null;
            Employer er = null;
            try
            {
                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction();
                    ee = new Employee();
                    ee.Name = ("Emmanuel");
                    s.Save(ee);
                    er = new Employer();
                    er.Name = ("RH");
                    s.Save(er);
                    tx.Commit();
                }

                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction();
                    ee = s.Get<Employee>(ee.Id);
                    ee.Name = ("Emmanuel2");
                    er = s.Get<Employer>(er.Id);
                    er.Name = ("RH2");
                    tx.Commit();
                }

                //Thread.Sleep(50);

                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction();
                    IFullTextSession fts = new Impl.FullTextSessionImpl(s);
                    QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "id", new StopAnalyzer(LuceneVersion.LUCENE_48));
                    Lucene.Net.Search.Query query = parser.Parse("name:emmanuel2");

                    bool results = fts.CreateFullTextQuery(query).List().Count > 0;
                    //don't test because in case of async, it query happens before actual saving
                    //if ( !results ) throw new Exception( "No results!" );
                    tx.Commit();
                }
                //System.Diagnostics.Debug.WriteLine("Interation " + worksCount + " completed on thread " + Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                errorsCount++;
            }
            finally
            {
                worksCount++;

                if (ee != null || er != null)
                    using (ISession s = OpenSession())
                    {
                        ITransaction tx = s.BeginTransaction();
                        if (ee != null)
                        {
                            ee = s.Get<Employee>(ee.Id);
                            if (ee != null)
                            {
                                s.Delete(ee);
                            }
                        }

                        if (er != null)
                        {
                            er = s.Get<Employer>(er.Id);
                            if (er != null)
                            {
                                s.Delete(er);
                            }
                        }

                        tx.Commit();
                    }
            }
        }

        private void ReverseWork(object state)
        {
            Employee ee = null;
            Employer er = null;
            try
            {
                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction();
                    er = new Employer();
                    er.Name = "RH";
                    s.Save(er);
                    ee = new Employee();
                    ee.Name = "Emmanuel";
                    s.Save(ee);
                    tx.Commit();
                }

                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction();
                    er = s.Get<Employer>(er.Id);
                    er.Name = ("RH2");
                    ee = s.Get<Employee>(ee.Id);
                    ee.Name = ("Emmanuel2");
                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                errorsCount++;
            }
            finally
            {
                reverseWorksCount++;

                if (ee != null || er != null)
                {
                    using (ISession s = OpenSession())
                    {
                        ITransaction tx = s.BeginTransaction();
                        if (ee != null)
                        {
                            ee = s.Get<Employee>(ee.Id);
                            if (ee != null)
                            {
                                s.Delete(ee);
                            }
                        }

                        if (er != null)
                        {
                            er = s.Get<Employer>(er.Id);
                            if (er != null)
                            {
                                s.Delete(er);
                            }
                        }

                        tx.Commit();
                    }
                }
            }
        }
    }
}