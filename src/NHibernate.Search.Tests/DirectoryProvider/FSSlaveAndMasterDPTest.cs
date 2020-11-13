using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NHibernate.Cfg;
using NHibernate.Search.Store;
using NUnit.Framework;

namespace NHibernate.Search.Tests.DirectoryProvider
{
    [TestFixture]
    public class FSSlaveAndMasterDPTest : MultiplySessionFactoriesTestCase
    {
        protected override IList Mappings
        {
            get { return new string[] { "DirectoryProvider.SnowStorm.hbm.xml" }; }
        }

        protected override int NumberOfSessionFactories
        {
            get { return 2; }
        }

        /// <summary>
        /// Verify that copies of the master get properly copied to the slaves.
        /// </summary>
        [Test]
        public void ProperCopy()
        {
            // Assert that the slave index is empty
            IFullTextSession fullTextSession = Search.CreateFullTextSession(GetSlaveSession());
            ITransaction tx = fullTextSession.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "id", new StopAnalyzer(LuceneVersion.LUCENE_48));
            IList result = fullTextSession.CreateFullTextQuery(parser.Parse("Location:texas")).List();
            Assert.AreEqual(0, result.Count, "No copy yet, fresh index expected");
            tx.Commit();
            fullTextSession.Close();

            // create an entity on the master and persist it in order to index it
            ISession session = CreateSession(0);
            tx = session.BeginTransaction();
            SnowStorm sn = new SnowStorm();
            sn.DateTime = DateTime.Now;
            sn.Location = ("Dallas, TX, USA");
            session.Persist(sn);
            tx.Commit();
            session.Close();

            int waitPeriodMilli = 2*1*1000 + 10; //wait a bit more than 2 refresh (one master / one slave)
            Thread.Sleep(waitPeriodMilli);

            // assert that the master has indexed the snowstorm
            fullTextSession = Search.CreateFullTextSession(GetMasterSession());
            tx = fullTextSession.BeginTransaction();
            result = fullTextSession.CreateFullTextQuery(parser.Parse("Location:dallas")).List();
            Assert.AreEqual(1, result.Count, "Original should get one");
            tx.Commit();
            fullTextSession.Close();

            // assert that index got copied to the slave as well
            fullTextSession = Search.CreateFullTextSession(GetSlaveSession());
            tx = fullTextSession.BeginTransaction();
            result = fullTextSession.CreateFullTextQuery(parser.Parse("Location:dallas")).List();
            Assert.AreEqual(1, result.Count, "First copy did not work out");
            tx.Commit();
            fullTextSession.Close();

            // add a new snowstorm to the master
            session = GetMasterSession();
            tx = session.BeginTransaction();
            sn = new SnowStorm();
            sn.DateTime = DateTime.Now;
            sn.Location = ("Chennai, India");
            session.Persist(sn);
            tx.Commit();
            session.Close();

            Thread.Sleep(waitPeriodMilli); //wait a bit more than 2 refresh (one master / one slave)

            // assert that the new snowstorm made it into the slave
            fullTextSession = Search.CreateFullTextSession(GetSlaveSession());
            tx = fullTextSession.BeginTransaction();
            result = fullTextSession.CreateFullTextQuery(parser.Parse("Location:chennai")).List();
            Assert.AreEqual(1, result.Count, "Second copy did not work out");
            tx.Commit();
            fullTextSession.Close();

            session = GetMasterSession();
            tx = session.BeginTransaction();
            sn = new SnowStorm();
            sn.DateTime = DateTime.Now;
            sn.Location = ("Melbourne, Australia");
            session.Persist(sn);
            tx.Commit();
            session.Close();

            Thread.Sleep(waitPeriodMilli); //wait a bit more than 2 refresh (one master / one slave)

            // once more - assert that the new snowstorm made it into the slave
            fullTextSession = Search.CreateFullTextSession(GetSlaveSession());
            tx = fullTextSession.BeginTransaction();
            result = fullTextSession.CreateFullTextQuery(parser.Parse("Location:melbourne")).List();
            Assert.AreEqual(1, result.Count, "Third copy did not work out");
            tx.Commit();
            fullTextSession.Close();
        }

        #region Helper methods

        public override void FixtureSetUp()
        {
            ZapLuceneStore();

            base.FixtureSetUp();
        }

        [TearDown]
        public void TearDown()
        {
            ZapLuceneStore();
        }

        protected override void Configure(IList<Configuration> cfg)
        {
            // master
            cfg[0].SetProperty("hibernate.search.default.sourceBase", "./lucenedirs/master/copy");
            cfg[0].SetProperty("hibernate.search.default.indexBase", "./lucenedirs/master/main");
            cfg[0].SetProperty("hibernate.search.default.refresh", "1"); // 1 sec
            cfg[0].SetProperty("hibernate.search.default.directory_provider", typeof(FSMasterDirectoryProvider).AssemblyQualifiedName);

            // slave(s)
            cfg[1].SetProperty("hibernate.search.default.sourceBase", "./lucenedirs/master/copy");
            cfg[1].SetProperty("hibernate.search.default.indexBase", "./lucenedirs/slave");
            cfg[1].SetProperty("hibernate.search.default.refresh", "1"); // 1sec
            cfg[1].SetProperty("hibernate.search.default.directory_provider", typeof(FSSlaveDirectoryProvider).AssemblyQualifiedName);
        }

        private ISession GetMasterSession()
        {
            return CreateSession(0);
        }

        private ISession GetSlaveSession()
        {
            return CreateSession(1);
        }

        private ISession CreateSession(int sessionFactoryNumber)
        {
            return SessionFactories[sessionFactoryNumber].OpenSession();
        }

        private void ZapLuceneStore()
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (Directory.Exists("./lucenedirs/"))
                    {
                        Directory.Delete("./lucenedirs/", true);
                    }
                }
                catch (IOException)
                {
                    // Wait for it to wind down for a while
                    Thread.Sleep(1000);
                }
            }
        }

        #endregion
    }
}