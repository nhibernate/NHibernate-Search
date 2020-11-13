using System;
using System.Collections;
using System.Threading;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Optimizer
{
    [TestFixture]
    public class OptimizerTestCase : PhysicalTestCase
    {
        private volatile int worksCount;
        private volatile int reverseWorksCount;
        private volatile int errorsCount;

        protected override IList Mappings
        {
            get { return new string[] { "Optimizer.Worker.hbm.xml", "Optimizer.Construction.hbm.xml" }; }
        }

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

            System.Diagnostics.Debug.WriteLine(iteration + " iterations (8 tx per iteration) in " + nThreads
                + " threads: " + new TimeSpan(DateTime.Now.Ticks - start).TotalSeconds + " secs; errorsCount = " + errorsCount);
            Assert.AreEqual(0, errorsCount, "Some iterations failed");
        }

        private void Work(object state)
        {
            Worker w = null;
            Construction c = null;
            try
            {
                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction(); // TODO: Once, it returned "null" and the session was already closed!
                    w = new Worker("Emmanuel", 65);
                    s.Save(w);
                    c = new Construction("Bellagio", "Las Vegas Nevada");
                    s.Save(c);
                    tx.Commit();
                }

                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction();
                    w = s.Get<Worker>(w.Id);
                    w.Name = "Gavin";
                    c = s.Get<Construction>(c.Id);
                    c.Name = "W Hotel";
                    tx.Commit();
                }

                Thread.Sleep(50);

                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction();
                    IFullTextSession fts = new Impl.FullTextSessionImpl(s);
                    QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "id", new StopAnalyzer(LuceneVersion.LUCENE_48));
                    Lucene.Net.Search.Query query = parser.Parse("name:Gavin");

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

                if (w != null || c != null)
                    using (ISession s = OpenSession())
                    {
                        ITransaction tx = s.BeginTransaction();
                        if (w != null)
                        {
                            w = s.Get<Worker>(w.Id);
                            if (w != null)
                                s.Delete(w);
                        }
                        if (c != null)
                        {
                            c = s.Get<Construction>(c.Id);
                            if (c != null)
                                s.Delete(c);
                        }
                        tx.Commit();
                    }
            }
        }

        private void ReverseWork(object state)
        {
            Worker w = null;
            Construction c = null;
            try
            {
                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction();
                    w = new Worker("Mladen", 70);
                    s.Save(w);
                    c = new Construction("Hover Dam", "Croatia");
                    s.Save(c);
                    tx.Commit();
                }

                using (ISession s = OpenSession())
                {
                    ITransaction tx = s.BeginTransaction();
                    w = s.Get<Worker>(w.Id);
                    w.Name = "Remi";
                    c = s.Get<Construction>(c.Id);
                    c.Name = "Palais des festivals";
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

                if (w != null || c != null)
                {
                    using (ISession s = OpenSession())
                    {
                        ITransaction tx = s.BeginTransaction();
                        if (w != null)
                        {
                            w = s.Get<Worker>(w.Id);
                            if (w != null)
                            {
                                s.Delete(w);
                            }
                        }

                        if (c != null)
                        {
                            c = s.Get<Construction>(c.Id);
                            if (c != null)
                            {
                                s.Delete(c);
                            }
                        }

                        tx.Commit();
                    }
                }
            }
        }
    }
}