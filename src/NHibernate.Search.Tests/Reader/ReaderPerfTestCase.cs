using System;
using System.Collections;
using System.Threading;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Reader
{
    [TestFixture]
    public class ReaderPerfTestCase : PhysicalTestCase
    {
        public bool insert = true;
        private volatile int worksCount;
        private volatile int reverseWorksCount;
        private volatile int errorsCount;

        #region Property methods

        protected override IList Mappings
        {
            get { return new string[] { "Reader.Detective.hbm.xml", "Reader.Suspect.hbm.xml" }; }
        }

        #endregion

        #region Tests

        [Test]
        public void Concurrency()
        {
            using (ISession s = OpenSession())
            {
                ITransaction tx = s.BeginTransaction();
                for (int index = 0; index < 500; index++) // Note: Was 5000
                {
                    Detective detective = new Detective();
                    detective.Name = "John Doe " + index;
                    detective.Badge = "123455" + index;
                    detective.PhysicalDescription = "Blond green eye etc etc";
                    s.Save(detective);
                    Suspect suspect = new Suspect();
                    suspect.Name = "Jane Doe " + index;
                    suspect.PhysicalDescription = "brunette, short, 30-ish";
                    if (index % 20 == 0)
                        suspect.SuspectCharge = "thief liar ";
                    else
                        suspect.SuspectCharge = " It's 1875 in London. The police have captured career criminal Montmorency. In the process he has been grievously wounded and it is up to a young surgeon to treat his wounds. During his recovery Montmorency learns of the city's new sewer system and sees in it the perfect underground highway for his thievery.  Washington Post columnist John Kelly recommends this title for middle schoolers, especially to be read aloud.";
                    s.Save(suspect);
                }
                tx.Commit();
            }
            Thread.Sleep(1000);

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

            System.Diagnostics.Debug.WriteLine(GetType().Name + ": " + iteration + " iterations in " + nThreads
                + " threads: " + new TimeSpan(DateTime.Now.Ticks - start).TotalSeconds + " secs; errorsCount = " + errorsCount);

            // Clean up
            using (ISession s = OpenSession())
            {
                ITransaction tx = s.BeginTransaction();
                s.Delete("from System.Object");
                tx.Commit();
            }

            Assert.AreEqual(0, errorsCount, "Some iterations failed");
        }

        #endregion

        #region Helper methods

        private void Work(object state)
        {
            //try
            //{
            //    Random random = new Random();
            //    QueryParser parser = new MultiFieldQueryParser(new string[] { "name", "physicalDescription", "suspectCharge" },
            //        new Lucene.Net.Analysis.Standard.StandardAnalyzer());
            //    using (ISession s = OpenSession())
            //    {
            //        ITransaction tx = s.BeginTransaction();
            //        IFullTextQuery query = GetQuery("John Doe", parser, s);
            //        Assert.IsTrue(query.ResultSize != 0);

            //        query = GetQuery("green", parser, s);
            //        random.Next(query.ResultSize - 15);
            //        query.SetFirstResult(random.Next(query.ResultSize - 15));
            //        query.SetMaxResults(10);
            //        query.List();
            //        tx.Commit();
            //    }

            //    using (ISession s = OpenSession())
            //    {
            //        ITransaction tx = s.BeginTransaction();
            //        IFullTextQuery query = GetQuery("John Doe", parser, s);
            //        Assert.IsTrue(query.ResultSize != 0);

            //        query = GetQuery("thief", parser, s);
            //        int firstResult = random.Next(query.ResultSize - 15);
            //        query.SetFirstResult(firstResult);
            //        query.SetMaxResults(10);
            //        IList result = query.List();
            //        object object_Renamed = result[0];
            //        if (insert && object_Renamed is Detective)
            //        {
            //            Detective detective = (Detective)object_Renamed;
            //            detective.PhysicalDescription = detective.PhysicalDescription + " Eye" + firstResult;
            //        }
            //        else if (insert && object_Renamed is Suspect)
            //        {
            //            Suspect suspect = (Suspect)object_Renamed;
            //            suspect.PhysicalDescription = suspect.PhysicalDescription + " Eye" + firstResult;
            //        }
            //        tx.Commit();
            //    }
            //    //System.Diagnostics.Debug.WriteLine("Interation " + worksCount + " completed on thread " + Thread.CurrentThread.ManagedThreadId);
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Debug.WriteLine(ex);
            //    errorsCount++;
            //}
            //finally
            //{
            //    worksCount++;
            //}
        }

        private void ReverseWork(object state)
        {
            //try
            //{
            //    Random random = new Random();
            //    QueryParser parser = new MultiFieldQueryParser(new string[] { "name", "physicalDescription", "suspectCharge" },
            //        new Lucene.Net.Analysis.Standard.StandardAnalyzer());
            //    IFullTextQuery query;
            //    using (ISession s = OpenSession())
            //    {
            //        ITransaction tx = s.BeginTransaction();
            //        query = GetQuery("John Doe", parser, s);
            //        Assert.IsTrue(query.ResultSize != 0);

            //        query = GetQuery("london", parser, s);
            //        random.Next(query.ResultSize - 15);
            //        query.SetFirstResult(random.Next(query.ResultSize - 15));
            //        query.SetMaxResults(10);
            //        query.List();
            //        tx.Commit();
            //    }

            //    using (ISession s = OpenSession())
            //    {
            //        ITransaction tx = s.BeginTransaction();
            //        GetQuery("John Doe", parser, s);
            //        Assert.IsTrue(query.ResultSize != 0);

            //        query = GetQuery("green", parser, s);
            //        random.Next(query.ResultSize - 15);
            //        query.SetFirstResult(random.Next(query.ResultSize - 15));
            //        query.SetMaxResults(10);
            //        query.List();
            //        tx.Commit();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Debug.WriteLine(ex);
            //    errorsCount++;
            //}
            //finally
            //{
            //    reverseWorksCount++;
            //}
        }

        private IFullTextQuery GetQuery(string queryString, QueryParser parser, ISession s)
        {
            Lucene.Net.Search.Query luceneQuery = null;
            try
            {
                luceneQuery = parser.Parse(queryString);
            }
            catch (ParseException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            return Search.CreateFullTextSession(s).CreateFullTextQuery(luceneQuery);
        }

        #endregion
    }
}