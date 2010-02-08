namespace NHibernate.Search.Tests.Cfg
{
    using Backend;

    using Impl;

    using NHibernate.Cfg;

    using NUnit.Framework;

    [TestFixture]
    public class LuceneIndexParametersTest : SearchTestCase
    {
        protected override System.Collections.IList Mappings
        {
            get { return new string[]
                             {
                             "DocumentTop.hbm.xml", 
                             "Query.Author.hbm.xml", 
                             "Query.Book.hbm.xml"
                             }; }
        }

        #region Tests

        [Test]
        public void UnsetBatchValueTakesTransaction()
        {
            IFullTextSession fullTextSession = Search.CreateFullTextSession(OpenSession());
            SearchFactoryImpl searchFactory = (SearchFactoryImpl) fullTextSession.SearchFactory;
            LuceneIndexingParameters indexingParameters = searchFactory.GetIndexingParameters(searchFactory.GetDirectoryProviders(typeof(DocumentTop))[0]);
            Assert.AreEqual(10, (int) indexingParameters.BatchIndexParameters.MergeFactor);
            Assert.AreEqual(1000, (int) indexingParameters.BatchIndexParameters.MaxBufferedDocs);
            Assert.AreEqual(99, (int)indexingParameters.BatchIndexParameters.TermIndexInterval);
            fullTextSession.Close();
        }

        [Test]
        public void BatchParametersDefault()
        {
            IFullTextSession fullTextSession = Search.CreateFullTextSession(OpenSession());
            SearchFactoryImpl searchFactory = (SearchFactoryImpl)fullTextSession.SearchFactory;
            LuceneIndexingParameters indexingParameters = searchFactory.GetIndexingParameters(searchFactory.GetDirectoryProviders(typeof(Query.Author))[0]);
            Assert.AreEqual(1, (int)indexingParameters.BatchIndexParameters.RamBufferSizeMb);
            Assert.AreEqual(9, (int)indexingParameters.BatchIndexParameters.MaxMergeDocs);
            Assert.AreEqual(1000, (int)indexingParameters.BatchIndexParameters.MaxBufferedDocs);
            Assert.AreEqual(10, (int)indexingParameters.BatchIndexParameters.MergeFactor);
            fullTextSession.Close();
        }

        [Test]
        public void TransactionParametersDefault()
        {
            IFullTextSession fullTextSession = Search.CreateFullTextSession(OpenSession());
            SearchFactoryImpl searchFactory = (SearchFactoryImpl)fullTextSession.SearchFactory;
            LuceneIndexingParameters indexingParameters = searchFactory.GetIndexingParameters(searchFactory.GetDirectoryProviders(typeof(Query.Author))[0]);
            Assert.AreEqual(2, (int)indexingParameters.TransactionIndexParameters.RamBufferSizeMb);
            Assert.AreEqual(9, (int)indexingParameters.TransactionIndexParameters.MaxMergeDocs);
            Assert.AreEqual(11, (int)indexingParameters.TransactionIndexParameters.MaxBufferedDocs);
            Assert.AreEqual(10, (int)indexingParameters.TransactionIndexParameters.MergeFactor);
            Assert.AreEqual(99, (int)indexingParameters.TransactionIndexParameters.TermIndexInterval);
            fullTextSession.Close();
        }

        [Test]
        public void BatchParameters()
        {
            IFullTextSession fullTextSession = Search.CreateFullTextSession(OpenSession());
            SearchFactoryImpl searchFactory = (SearchFactoryImpl)fullTextSession.SearchFactory;
            LuceneIndexingParameters indexingParameters = searchFactory.GetIndexingParameters(searchFactory.GetDirectoryProviders(typeof(Query.Book))[0]);
            Assert.AreEqual(3, (int)indexingParameters.BatchIndexParameters.RamBufferSizeMb);
            Assert.AreEqual(12, (int)indexingParameters.BatchIndexParameters.MaxMergeDocs);
            Assert.AreEqual(14, (int)indexingParameters.BatchIndexParameters.MaxBufferedDocs);
            Assert.AreEqual(13, (int)indexingParameters.BatchIndexParameters.MergeFactor);
            Assert.AreEqual(100, (int)indexingParameters.BatchIndexParameters.TermIndexInterval);
            fullTextSession.Close();
        }

        [Test]
        public void TransactionParameters()
        {
            IFullTextSession fullTextSession = Search.CreateFullTextSession(OpenSession());
            SearchFactoryImpl searchFactory = (SearchFactoryImpl)fullTextSession.SearchFactory;
            LuceneIndexingParameters indexingParameters = searchFactory.GetIndexingParameters(searchFactory.GetDirectoryProviders(typeof(Query.Book))[0]);
            Assert.AreEqual(4, (int)indexingParameters.TransactionIndexParameters.RamBufferSizeMb);
            Assert.AreEqual(15, (int)indexingParameters.TransactionIndexParameters.MaxMergeDocs);
            Assert.AreEqual(17, (int)indexingParameters.TransactionIndexParameters.MaxBufferedDocs);
            Assert.AreEqual(16, (int)indexingParameters.TransactionIndexParameters.MergeFactor);
            Assert.AreEqual(101, (int)indexingParameters.TransactionIndexParameters.TermIndexInterval);
            fullTextSession.Close();
        }

        #endregion

        #region Helper methods

        protected override void Configure(Configuration cfg)
        {
            base.Configure(cfg);

            cfg.SetProperty("hibernate.search.default.batch.ram_buffer_size", "1");

            cfg.SetProperty("hibernate.search.default.transaction.ram_buffer_size", "2");
            cfg.SetProperty("hibernate.search.default.transaction.max_merge_docs", "9");
            cfg.SetProperty("hibernate.search.default.transaction.merge_factor", "10");
            cfg.SetProperty("hibernate.search.default.transaction.max_buffered_docs", "11");
            cfg.SetProperty("hibernate.search.default.transaction.term_index_interval", "99");

            cfg.SetProperty("hibernate.search.Book.batch.ram_buffer_size", "3");
            cfg.SetProperty("hibernate.search.Book.batch.max_merge_docs", "12");
            cfg.SetProperty("hibernate.search.Book.batch.merge_factor", "13");
            cfg.SetProperty("hibernate.search.Book.batch.max_buffered_docs", "14");
            cfg.SetProperty("hibernate.search.Book.batch.term_index_interval", "100");

            cfg.SetProperty("hibernate.search.Book.transaction.ram_buffer_size", "4");
            cfg.SetProperty("hibernate.search.Book.transaction.max_merge_docs", "15");
            cfg.SetProperty("hibernate.search.Book.transaction.merge_factor", "16");
            cfg.SetProperty("hibernate.search.Book.transaction.max_buffered_docs", "17");
            cfg.SetProperty("hibernate.search.Book.transaction.term_index_interval", "101");

            cfg.SetProperty("hibernate.search.Documents.ram_buffer_size", "4");
        }

        #endregion
    }
}
