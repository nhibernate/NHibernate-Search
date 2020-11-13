using System.Collections;
using System.IO;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NHibernate.Cfg;
using NHibernate.Search.Store;

using NUnit.Framework;

namespace NHibernate.Search.Tests.Shards
{
    [TestFixture]
    public class ShardsTest : PhysicalTestCase
    {
        protected override IList Mappings
        {
            get
            {
                return new[]
                           {
                             "Shards.Animal.hbm.xml",
                             "Shards.Furniture.hbm.xml"
                             };
            }
        }

        protected override bool RunFixtureSetUpAndTearDownForEachTest
        {
            get { return true; }
        }

        #region Tests

        [Test]
        public void IdShardingStrategy()
        {
            IDirectoryProvider[] dps = new IDirectoryProvider[] { new RAMDirectoryProvider(), new RAMDirectoryProvider() };
            IdHashShardingStrategy shardingStrategy = new IdHashShardingStrategy();
            shardingStrategy.Initialize(null, dps);
            Assert.AreSame(dps[1], shardingStrategy.GetDirectoryProviderForAddition(typeof(Animal), 1, "1", null));
            Assert.AreSame(dps[0], shardingStrategy.GetDirectoryProviderForAddition(typeof(Animal), 2, "2", null));
        }

        [Test]
        public void StandardBehavior()
        {
            ISession s = OpenSession();
            ITransaction tx = s.BeginTransaction();
            Animal a = new Animal();
            a.Id = 1;
            a.Name = "Elephant";
            s.Persist(a);
            a = new Animal();
            a.Id = 2;
            a.Name = "Bear";
            s.Persist(a);
            tx.Commit();

            s.Clear();

            tx = s.BeginTransaction();
            a = (Animal)s.Get(typeof(Animal), 1);
            a.Name = "Mouse";
            Furniture fur = new Furniture();
            fur.Color = "dark blue";
            s.Persist(fur);
            tx.Commit();

            s.Clear();

            tx = s.BeginTransaction();
            IFullTextSession fts = Search.CreateFullTextSession(s);
            var version = LuceneVersion.LUCENE_48;
            QueryParser parser = new QueryParser(version, "id", new StopAnalyzer(version));

            IList results = fts.CreateFullTextQuery(parser.Parse("name:mouse OR name:bear")).List();
            Assert.AreEqual(2, results.Count, "Either double insert, single update, or query fails with shards");

            results = fts.CreateFullTextQuery(parser.Parse("name:mouse OR name:bear OR color:blue")).List();
            Assert.AreEqual(3, results.Count, "Mixing shared and non sharded properties fails");
            results = fts.CreateFullTextQuery(parser.Parse("name:mouse OR name:bear OR color:blue")).List();
            Assert.AreEqual(3, results.Count, "Mixing shared and non sharded properties fails with indexreader reuse");

            // cleanup
            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        public void InternalSharding()
        {
            //ISession s = OpenSession();
            //ITransaction tx = s.BeginTransaction();
            //Animal a = new Animal();
            //a.Id = 1;
            //a.Name = "Elephant";
            //s.Persist(a);
            //a = new Animal();
            //a.Id = 2;
            //a.Name = "Bear";
            //s.Persist(a);
            //tx.Commit();

            //s.Clear();

            //IndexReader reader = IndexReader.Open(new FileInfo(BaseIndexDir.FullName + "\\Animal00"));
            //try
            //{
            //    int num = reader.NumDocs;
            //    Assert.AreEqual(1, num);
            //}
            //finally
            //{
            //    reader.Dispose();
            //}

            //reader = IndexReader.Open(new FileInfo(BaseIndexDir.FullName + "\\Animal.1"));
            //try
            //{
            //    int num = reader.NumDocs;
            //    Assert.AreEqual(1, num);
            //}
            //finally
            //{
            //    reader.Dispose();
            //}

            //tx = s.BeginTransaction();
            //a = (Animal) s.Get(typeof(Animal),1);
            //a.Name = "Mouse";
            //tx.Commit();

            //s.Clear();

            //reader = IndexReader.Open(new FileInfo(BaseIndexDir.FullName + "\\Animal.1"));
            //try
            //{
            //    int num = reader.NumDocs();
            //    Assert.AreEqual(1, num);
            //    TermDocs docs = reader.GetTermDocs(new Term("name", "mouse"));
            //    Assert.IsTrue(docs.Next());
            //    Document doc = reader.Document(docs.Doc());
            //    Assert.IsFalse(docs.Next());
            //}
            //finally
            //{
            //    reader.Dispose();
            //}

            //tx = s.BeginTransaction();
            //IFullTextSession fts = Search.CreateFullTextSession(s);
            //QueryParser parser = new QueryParser("id", new StopAnalyzer(LuceneVersion.LUCENE_48));

            //IList results = fts.CreateFullTextQuery(parser.Parse("name:mouse OR name:bear")).List();
            //Assert.AreEqual(2, results.Count, "Either double insert, single update, or query fails with shards");

            //// cleanup
            //s.Delete("from System.Object");
            //tx.Commit();
            //s.Close();
        }

        #endregion

        #region Setup/Teardown

        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);

            // is the default when multiple shards are set up
            // configure.setProperty( "hibernate.search.Animal.sharding_strategy", IdHashShardingStrategy.class );
            configuration.SetProperty("hibernate.search.Animal.sharding_strategy.nbr_of_shards", "2");
            configuration.SetProperty("hibernate.search.Animal.0.indexName", "Animal00");
        }

        #endregion
    }
}