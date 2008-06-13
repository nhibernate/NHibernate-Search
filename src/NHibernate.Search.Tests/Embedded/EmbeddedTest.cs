using System.Collections;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
//using NHibernate.Cfg;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Embedded
{
    [TestFixture, Ignore("Not implemented yet")]
    public class EmbeddedTest : SearchTestCase
    {
        #region Helper methods

        //protected override void Configure(Configuration configuration)
        //{
        //    base.Configure(configuration);
        //    // TODO: Set up listeners!
        //}

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

        #endregion

        #region Tests

        [Test, Ignore("Not implemented yet")]
        public void TestEmbeddedIndexing()
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


		    IFullTextSession session = Search.CreateFullTextSession( s );
		    QueryParser parser = new QueryParser( "id", new StandardAnalyzer() );

            Lucene.Net.Search.Query query = parser.Parse( "address.street:place" );
		    IList result = session.CreateFullTextQuery( query ).List();
		    Assert.AreEqual( 1, result.Count, "unable to find property in embedded" );

		    query = parser.Parse( "address.ownedBy_name:renting" );
		    result = session.CreateFullTextQuery( query).List();
		    Assert.AreEqual(  1, result.Count, "unable to find property in embedded" );

		    query = parser.Parse( "address.id:" + a.Id );
		    result = session.CreateFullTextQuery( query ).List();
		    Assert.AreEqual( 1, result.Count, "unable to find property by id of embedded" );

		    query = parser.Parse( "address.country.name:" + a.Country.Name );
		    result = session.CreateFullTextQuery( query ).List();
		    Assert.AreEqual(  1, result.Count, "unable to find property with 2 levels of embedded" );

		    s.Clear();

		    tx = s.BeginTransaction();
		    Address address = s.Get<Address>( a.Id );
		    address.OwnedBy.Name = "Buckhead community" ;
		    tx.Commit();

		    s.Clear();

		    session = Search.CreateFullTextSession( s );

		    query = parser.Parse( "address.ownedBy_name:buckhead" );
		    result = session.CreateFullTextQuery( query ).List();
		    Assert.AreEqual( 1, result.Count,"change in embedded not reflected in root index" );

		    s.Clear();

            // Tidy up
            tx = s.BeginTransaction();
            s.Delete(tower);
            s.Delete(a);
            s.Delete(c);
            tx.Commit();

            s.Close();
        }

        [Test, Ignore("Not implemented yet")]
        public void TestContainedIn()
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
		    Address address =  s.Get<Address>(a.Id );
		    address.Street = "Peachtree Road NE" ;
		    tx.Commit();

		    s.Clear();

		    IFullTextSession session = Search.CreateFullTextSession( s );
		    QueryParser parser = new QueryParser( "id", new StandardAnalyzer() );

            Lucene.Net.Search.Query query = parser.Parse( "address.street:peachtree" );
		    IList result = session.CreateFullTextQuery( query).List();
		    Assert.AreEqual( 1, result.Count, "change in embedded not reflected in root index" );

		    s.Clear();

		    tx = s.BeginTransaction();
		    address = s.Get<Address>( a.Id );
            IEnumerator en = address.Towers.GetEnumerator();
            en.MoveNext();
            Tower tower1 = (Tower) en.Current;
		    tower1.Address = null ;
		    address.Towers.Remove( tower1 );
		    tx.Commit();

		    s.Clear();

		    session = Search.CreateFullTextSession( s );

		    query = parser.Parse( "address.street:peachtree" );
		    result = session.CreateFullTextQuery( query ).List();
		    Assert.AreEqual( 0, result.Count,"breaking link fails" );

            // Tidy up
            tx = s.BeginTransaction();
            s.Delete(tower);
            s.Delete(a);
            tx.Commit();

            s.Close();
        }

        [Test, Ignore("Not implemented yet")]
        public void TestIndexedEmbeddedAndCollections()
        {
            Author a = new Author();
            a.Name = "Voltaire";
            Author a2 = new Author();
            a2.Name = "Victor Hugo";
            Author a3 = new Author();
            a3.Name = "Moliere";
            Author a4 = new Author();
            a4.Name = "Proust";

            Order o = new Order();
            o.OrderNumber = "ACVBNM";

            Order o2 = new Order();
            o2.OrderNumber = "ZERTYD";

            Product p1 = new Product();
            p1.Name = "Candide";
            p1.Authors.Add(a);
            p1.Authors.Add(a2); //be creative

            Product p2 = new Product();
            p2.Name = "Le malade imaginaire";
            p2.Authors.Add(a3);
            p2.Orders.Add("Emmanuel", o);
            p2.Orders.Add("Gavin", o2);


            ISession s = OpenSession();
            ITransaction tx = s.BeginTransaction();
            s.Persist(a);
            s.Persist(a2);
            s.Persist(a3);
            s.Persist(a4);
            s.Persist(o);
            s.Persist(o2);
            s.Persist(p1);
            s.Persist(p2);
            tx.Commit();

            s.Clear();

		    IFullTextSession session = Search.CreateFullTextSession( s );
		    tx = session.BeginTransaction();

		    QueryParser parser = new MultiFieldQueryParser( new string[] { "name", "authors.name" }, new StandardAnalyzer() );

            Lucene.Net.Search.Query query = parser.Parse( "Hugo" );
		    IList result = session.CreateFullTextQuery( query ).List();
		    Assert.AreEqual( 1, result.Count, "collection of embedded ignored" );

		    //update the collection
		    Product p = (Product) result[0];
		    p.Authors.Add( a4 );

		    //PhraseQuery
		    query = new TermQuery( new Term( "orders.orderNumber", "ZERTYD" ) );
		    result = session.CreateFullTextQuery( query).List();
		    Assert.AreEqual( 1, result.Count, "collection of untokenized ignored" );
		    query = new TermQuery( new Term( "orders.orderNumber", "ACVBNM" ) );
		    result = session.CreateFullTextQuery( query).List();
		    Assert.AreEqual( 1, result.Count, "collection of untokenized ignored" );

		    tx.Commit();

		    s.Clear();

		    tx = s.BeginTransaction();
		    session = Search.CreateFullTextSession( s );
		    query = parser.Parse( "Proust" );
		    result = session.CreateFullTextQuery( query ).List();
		    //HSEARCH-56
		    Assert.AreEqual( 1, result.Count, "update of collection of embedded ignored" );

            // Tidy up
            s.Delete(a);
            s.Delete(a2);
            s.Delete(a3);
            s.Delete(a4);
            s.Delete(o);
            s.Delete(o2);
            s.Delete(p1);
            s.Delete(p2);
            tx.Commit();

            s.Close();
        }
        #endregion
    }
}