using System.Collections;

using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Embedded
{
    /// <summary>
    /// The embedded test.
    /// </summary>
    [TestFixture]
    public class EmbeddedTest : SearchTestCase
    {
        #region Helper methods

        // protected override void Configure(Configuration configuration)
        // {
        // base.Configure(configuration);
        // // TODO: Set up listeners!
        // }

        /// <summary>
        /// Gets Mappings.
        /// </summary>
        protected override IList Mappings
        {
            get
            {
                return new[]
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

        #endregion

        #region Tests

        /// <summary>
        /// The embedded indexing.
        /// </summary>
        [Test]
        public void EmbeddedIndexing()
        {
            Tower tower = new Tower();
            tower.Name = "JBoss tower";
            Address a = new Address();
            a.Street = "Tower place";
            a.Towers.Add(tower);
            tower.Address = a;
            Owner o = new Owner();
            o.Name = "Atlanta Renting corp";
            a.OwnedBy = o;
            o.Address = a;
            Country c = new Country();
            c.Name = "France";
            a.Country = c;

            ISession s = OpenSession();
            ITransaction tx = s.BeginTransaction();
            s.Persist(tower);
            tx.Commit();

            IFullTextSession session = Search.CreateFullTextSession(s);
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "id", new StandardAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("address.street:place");
            IList result = session.CreateFullTextQuery(query).List();
            Assert.AreEqual(1, result.Count, "unable to find property in embedded");

            query = parser.Parse("address.ownedBy_name:renting");
            result = session.CreateFullTextQuery(query).List();
            Assert.AreEqual(1, result.Count, "unable to find property in embedded");

            query = parser.Parse("address.id:" + a.Id);
            result = session.CreateFullTextQuery(query).List();
            Assert.AreEqual(1, result.Count, "unable to find property by id of embedded");

            query = parser.Parse("address.country.name:" + a.Country.Name);
            result = session.CreateFullTextQuery(query).List();
            Assert.AreEqual(1, result.Count, "unable to find property with 2 levels of embedded");

            s.Clear();

            tx = s.BeginTransaction();
            Address address = s.Get<Address>(a.Id);
            address.OwnedBy.Name = "Buckhead community";

            // NB Not in the Java?
            s.Persist(address);
            tx.Commit();

            s.Clear();

            session = Search.CreateFullTextSession(s);

            query = parser.Parse("address.ownedBy_name:buckhead");
            result = session.CreateFullTextQuery(query).List();
            Assert.AreEqual(1, result.Count, "change in embedded not reflected in root index");

            s.Clear();

            // Tidy up
            tx = s.BeginTransaction();
            s.Delete(tower);
            s.Delete(a);
            s.Delete(c);
            tx.Commit();

            s.Close();
        }

        /// <summary>
        /// The contained in.
        /// </summary>
        [Test]
        public void ContainedIn()
        {
            Tower tower = new Tower();
            tower.Name = "JBoss tower";
            Address a = new Address();
            a.Street = "Tower place";
            a.Towers.Add(tower);
            tower.Address = a;
            Owner o = new Owner();
            o.Name = "Atlanta Renting corp";
            a.OwnedBy = o;
            o.Address = a;

            ISession s = OpenSession();
            ITransaction tx = s.BeginTransaction();
            s.Persist(tower);
            tx.Commit();

            s.Clear();

            tx = s.BeginTransaction();
            Address address = s.Get<Address>(a.Id);
            address.Street = "Peachtree Road NE";
            tx.Commit();

            s.Clear();

            IFullTextSession session = Search.CreateFullTextSession(s);
            var version = LuceneVersion.LUCENE_48;
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "id", new StandardAnalyzer(version));

            Lucene.Net.Search.Query query = parser.Parse("address.street:peachtree");
            IList result = session.CreateFullTextQuery(query).List();
            Assert.AreEqual(1, result.Count, "change in embedded not reflected in root index");

            s.Clear();

            tx = s.BeginTransaction();
            address = s.Get<Address>(a.Id);
            IEnumerator en = address.Towers.GetEnumerator();
            en.MoveNext();
            Tower tower1 = (Tower) en.Current;
            tower1.Address = null;
            address.Towers.Remove(tower1);
            tx.Commit();

            s.Clear();

            session = Search.CreateFullTextSession(s);

            query = parser.Parse("address.street:peachtree");
            result = session.CreateFullTextQuery(query).List();
            Assert.AreEqual(0, result.Count, "breaking link fails");

            // Tidy up
            tx = s.BeginTransaction();
            s.Delete(tower);
            s.Delete(a);
            tx.Commit();

            s.Close();
        }

        #endregion
    }
}