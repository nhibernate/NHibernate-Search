using System.Collections;
using NHibernate.Cfg;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Embedded
{
    [TestFixture]
    public class EmbeddedTest : SearchTestCase
    {
        #region Helper methods

        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            // TODO: Set up listeners!
        }

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

        [Test]
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

            tx = s.BeginTransaction();
            s.Delete(tower);
            s.Delete(a);
            s.Delete(c);
            tx.Commit();

            s.Close();
        }

        [Test]
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

            tx = s.BeginTransaction();
            s.Delete(tower);
            s.Delete(a);
            tx.Commit();

            s.Close();
        }

        [Test]
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

            //s.Clear();

            tx = s.BeginTransaction();
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