using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Embedded
{
    using System.Collections;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;
    using NUnit.Framework;

    [TestFixture]
    public class IndexedEmbeddedAndCollections : SearchTestCase
    {
        private Author a;
        private Author a2;
        private Author a3;
        private Author a4;
        private Order o;
        private Order o2;
        private Product p1;
        private Product p2;
        private ISession s;
        private ITransaction tx;

        protected override IList Mappings
        {
            get
            {
                return new string[]
                {
                    "Embedded.Tower.hbm.xml",
                    "Embedded.Address.hbm.xml",
                    "Embedded.Product.hbm.xml",
                    "Embedded.Order.hbm.xml",
                    "Embedded.Author.hbm.xml",
                    "Embedded.Country.hbm.xml"
                };
            }
        }

        protected override void OnSetUp()
        {
            base.OnSetUp();

            a = new Author();
            a.Name = "Voltaire";
            a2 = new Author();
            a2.Name = "Victor Hugo";
            a3 = new Author();
            a3.Name = "Moliere";
            a4 = new Author();
            a4.Name = "Proust";

            o = new Order();
            o.OrderNumber = "ACVBNM";

            o2 = new Order();
            o2.OrderNumber = "ZERTYD";

            p1 = new Product();
            p1.Name = "Candide";
            p1.Authors.Add(a);
            p1.Authors.Add(a2); //be creative

            p2 = new Product();
            p2.Name = "Le malade imaginaire";
            p2.Authors.Add(a3);
            p2.Orders.Add("Emmanuel", o);
            p2.Orders.Add("Gavin", o2);

            s = OpenSession();
            tx = s.BeginTransaction();
            s.Persist(a);
            s.Persist(a2);
            s.Persist(a3);
            s.Persist(a4);
            s.Persist(o);
            s.Persist(o2);
            s.Persist(p1);
            s.Persist(p2);
            tx.Commit();

            tx = s.BeginTransaction();

            s.Clear();
        }

        protected override void OnTearDown()
        {
            // Tidy up
            s.Delete("from System.Object");

            tx.Commit();

            s.Close();

            base.OnTearDown();
        }

        [Test]
        public void CanLookupEntityByValueOfEmbeddedSetValues()
        {
            IFullTextSession session = Search.CreateFullTextSession(s);

            var version = LuceneVersion.LUCENE_48;
            QueryParser parser = new MultiFieldQueryParser(version, new string[] { "name", "authors.name" }, new StandardAnalyzer(version));

            Lucene.Net.Search.Query query = parser.Parse("Hugo");
            IList result = session.CreateFullTextQuery(query).List();
            Assert.AreEqual(1, result.Count, "collection of embedded (set) ignored");
        }

        [Test]
        public void CanLookupEntityByValueOfEmbeddedDictionaryValue()
        {
            IFullTextSession session = Search.CreateFullTextSession(s);
            
            // PhraseQuery
            TermQuery query = new TermQuery(new Term("orders.orderNumber", "ZERTYD"));
            IList result = session.CreateFullTextQuery(query, typeof(Product)).List();
            Assert.AreEqual(1, result.Count, "collection of untokenized ignored");

            query = new TermQuery(new Term("orders.orderNumber", "ACVBNM"));
            result = session.CreateFullTextQuery(query, typeof(Product)).List();
            Assert.AreEqual(1, result.Count, "collection of untokenized ignored");
        }

        [Test]
        public void CanLookupEntityByUpdatedValueInSet()
        {
            Product p = s.Get<Product>(p1.Id);
            p.Authors.Add(s.Get<Author>(a4.Id));
            tx.Commit();

            s.Clear();

            tx = s.BeginTransaction();

            IFullTextSession session = Search.CreateFullTextSession(s);
            var version = LuceneVersion.LUCENE_48;
            QueryParser parser = new MultiFieldQueryParser(version, new string[] { "name", "authors.name" }, new StandardAnalyzer(version));
            Query query = parser.Parse("Proust");
            IList result = session.CreateFullTextQuery(query, typeof(Product)).List();

            // HSEARCH-56
            Assert.AreEqual(1, result.Count, "update of collection of embedded ignored");
        }
    }
}