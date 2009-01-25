using System;
using System.Collections;
using System.IO;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers;
using NHibernate.Cfg;
using NHibernate.Search.Store;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Optimizer
{
    [TestFixture]
    public class OptimizerTestCase : SearchTestCase
    {
        private volatile int worksCount;
        private volatile int reverseWorksCount;
        private volatile int errorsCount;

        protected virtual string BaseIndexDirName
        {
            get { return "indextemp"; }
        }

        private FileInfo BaseIndexDir
        {
            get
            {
                FileInfo current = new FileInfo(".");
                FileInfo sub = new FileInfo(current.FullName + "\\" + BaseIndexDirName);
                return sub;
            }
        }

        protected override IList Mappings
        {
            get { return new string[] { "Optimizer.Worker.hbm.xml", "Optimizer.Construction.hbm.xml" }; }
        }

        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            FileInfo sub = BaseIndexDir;
            try
            {
                Delete(sub);
            }
            catch (IOException ex) // TODO: Find a way to dispose Lucene.Net so that it closes all its files
            {
                System.Diagnostics.Debug.WriteLine(ex); // "The process cannot access the file '_0.cfs' because it is being used by another process."
            }
            Directory.CreateDirectory(sub.FullName);

            configuration.SetProperty("hibernate.search.default.indexBase", sub.FullName);
            configuration.SetProperty("hibernate.search.default.directory_provider", typeof(FSDirectoryProvider).AssemblyQualifiedName);
            configuration.SetProperty(Environment.AnalyzerClass, typeof(StopAnalyzer).AssemblyQualifiedName);
        }

        protected override void OnTearDown()
        {
            base.OnTearDown();

            FileInfo sub = BaseIndexDir;
            try
            {
                Delete(sub);
            }
            catch(IOException ex) // TODO: Find a way to dispose Lucene.Net so that it closes all its files
            {
                System.Diagnostics.Debug.WriteLine(ex); // "The process cannot access the file '_0.cfs' because it is being used by another process."
            }
        }

        private void Delete(FileInfo sub)
        {
            if (Directory.Exists(sub.FullName))
                Directory.Delete(sub.FullName, true);
            else
                File.Delete(sub.FullName);
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
            try
            {
                ISession s = OpenSession();
                ITransaction tx = s.BeginTransaction(); // TODO: Once, it returned "null" and the session was already closed!
                Worker w = new Worker("Emmanuel", 65);
                s.Save(w);
                Construction c = new Construction("Bellagio", "Las Vegas Nevada");
                s.Save(c);
                tx.Commit();
                s.Close();

                s = OpenSession();
                tx = s.BeginTransaction();
                w = s.Get<Worker>(w.Id);
                w.Name = "Gavin";
                c = s.Get<Construction>(c.Id);
                c.Name = "W Hotel";
                tx.Commit();
                s.Close();

                Thread.Sleep(50);

                s = OpenSession();
                tx = s.BeginTransaction();
                IFullTextSession fts = new Impl.FullTextSessionImpl(s);
                QueryParser parser = new QueryParser("id", new StopAnalyzer());
                Lucene.Net.Search.Query query = parser.Parse("name:Gavin");

                bool results = fts.CreateFullTextQuery(query).List().Count > 0;
                //don't test because in case of async, it query happens before actual saving
                //if ( !results ) throw new Exception( "No results!" );
                tx.Commit();
                s.Close();

                s = OpenSession();
                tx = s.BeginTransaction();
                w = s.Get<Worker>(w.Id);
                s.Delete(w);
                c = s.Get<Construction>(c.Id);
                s.Delete(c);
                tx.Commit();
                s.Close();
                System.Diagnostics.Debug.WriteLine("Interation " + worksCount + " completed on thread " + Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                errorsCount++;
            }
            finally
            {
                worksCount++;
            }
        }

        private void ReverseWork(object state)
        {
            try
            {
                ISession s = OpenSession();
                ITransaction tx = s.BeginTransaction();
                Worker w = new Worker("Mladen", 70);
                s.Save(w);
                Construction c = new Construction("Hover Dam", "Croatia");
                s.Save(c);
                tx.Commit();
                s.Close();

                s = OpenSession();
                tx = s.BeginTransaction();
                w = s.Get<Worker>(w.Id);
                w.Name = "Remi";
                c = s.Get<Construction>(c.Id);
                c.Name = "Palais des festivals";
                tx.Commit();
                s.Close();

                s = OpenSession();
                tx = s.BeginTransaction();
                w = s.Get<Worker>(w.Id);
                s.Delete(w);
                c = s.Get<Construction>(c.Id);
                s.Delete(c);
                tx.Commit();
                s.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                errorsCount++;
            }
            finally
            {
                reverseWorksCount++;
            }
        }
    }
}