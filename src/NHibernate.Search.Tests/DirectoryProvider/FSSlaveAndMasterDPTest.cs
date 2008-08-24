using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers;
using NHibernate.Cfg;
using NHibernate.Search.Store;
using NUnit.Framework;

namespace NHibernate.Search.Tests.DirectoryProvider
{
    [TestFixture]
    public class FSSlaveAndMasterDPTest : MultiplySessionFactoriesTestCase
    {
        #region Private methods

        private void ZapLuceneStore()
        {
            try
            {
                if (Directory.Exists("./lucenedirs/"))
                    Directory.Delete("./lucenedirs/", true);

            }
            catch (IOException)
            {
                // Wait for it to wind down for a while
                Thread.Sleep(1000);
            }
        }

        #endregion

        #region Setup/Teardown

        [TearDown]
        protected void TearDown()
        {
            ZapLuceneStore();
        }

        #endregion

        public override void FixtureSetUp()
        {
            ZapLuceneStore();
            Directory.CreateDirectory("./lucenedirs/master/copy");
            Directory.CreateDirectory("./lucenedirs/master/main");
            Directory.CreateDirectory("./lucenedirs/slave");

            base.FixtureSetUp();
        }

        private ISession CreateSession(int sessionFactoryNumber)
        {
            return SessionFactories[sessionFactoryNumber].OpenSession();
        }

        protected override void Configure(IList<Configuration> cfg)
        {
            //master
            cfg[0].SetProperty("hibernate.search.default.sourceBase", "./lucenedirs/master/copy");
            cfg[0].SetProperty("hibernate.search.default.indexBase", "./lucenedirs/master/main");
            cfg[0].SetProperty("hibernate.search.default.refresh", "1"); // 1 sec
            cfg[0].SetProperty("hibernate.search.default.directory_provider",
                               typeof(FSMasterDirectoryProvider).AssemblyQualifiedName);
            cfg[0].Configure();

            //slave(s)
            cfg[1].SetProperty("hibernate.search.default.sourceBase", "./lucenedirs/master/copy");
            cfg[1].SetProperty("hibernate.search.default.indexBase", "./lucenedirs/slave");
            cfg[1].SetProperty("hibernate.search.default.refresh", "1"); // 1sec
            cfg[1].SetProperty("hibernate.search.default.directory_provider",
                               typeof(FSSlaveDirectoryProvider).AssemblyQualifiedName);
            cfg[0].Configure();
        }

        protected override IList Mappings
        {
            get { return new string[] {"DirectoryProvider.SnowStorm.hbm.xml"}; }
        }

        protected override int NumberOfSessionFactories
        {
            get { return 2; }
        }

        [Test]
        public void ProperCopy()
        {
            ISession s1 = CreateSession(0);
            SnowStorm sn = new SnowStorm();
            sn.DateTime = DateTime.Now;
            sn.Location = ("Dallas, TX, USA");

            IFullTextSession fts2 = Search.CreateFullTextSession(CreateSession(1));
            QueryParser parser = new QueryParser("id", new StopAnalyzer());
            IList result = fts2.CreateFullTextQuery(parser.Parse("Location:texas")).List();
            Assert.AreEqual(0, result.Count, "No copy yet, fresh index expected");

            s1.Save(sn);
            s1.Flush(); //we don' commit so we need to flush manually

            fts2.Close();
            s1.Close();

            int waitPeriod = 2*1*1000 + 10; //wait a bit more than 2 refresh (one master / one slave)
            Thread.Sleep(waitPeriod);

            //temp test original
            fts2 = Search.CreateFullTextSession(CreateSession(0));
            result = fts2.CreateFullTextQuery(parser.Parse("Location:dallas")).List();
            Assert.AreEqual(1, result.Count, "Original should get one");

            fts2 = Search.CreateFullTextSession(CreateSession(1));
            result = fts2.CreateFullTextQuery(parser.Parse("Location:dallas")).List();
            Assert.AreEqual(1, result.Count, "First copy did not work out");

            s1 = CreateSession(0);
            sn = new SnowStorm();
            sn.DateTime = DateTime.Now;
            sn.Location = ("Chennai, India");

            s1.Save(sn);
            s1.Flush(); //we don' commit so we need to flush manually

            fts2.Close();
            s1.Close();

            Thread.Sleep(waitPeriod); //wait a bit more than 2 refresh (one master / one slave)

            fts2 = Search.CreateFullTextSession(CreateSession(1));
            result = fts2.CreateFullTextQuery(parser.Parse("Location:chennai")).List();
            Assert.AreEqual(1, result.Count, "Second copy did not work out");

            s1 = CreateSession(0);
            sn = new SnowStorm();
            sn.DateTime = DateTime.Now;
            sn.Location = ("Melbourne, Australia");

            s1.Save(sn);
            s1.Flush(); //we don' commit so we need to flush manually

            fts2.Close();
            s1.Close();

            Thread.Sleep(waitPeriod); //wait a bit more than 2 refresh (one master / one slave)

            fts2 = Search.CreateFullTextSession(CreateSession(1));
            result = fts2.CreateFullTextQuery(parser.Parse("Location:melbourne")).List();
            Assert.AreEqual(1, result.Count, "Third copy did not work out");

            fts2.Close();
        }
    }
}