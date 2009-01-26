using System.Collections;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Worker
{
    [TestFixture]
    public class ConcurrencyTest : SearchTestCase
    {
        protected override IList Mappings
        {
            get { return new string[] { "Worker.Drink.hbm.xml", "Worker.Food.hbm.xml" }; }
        }

        [Test]
        public void MultipleEntitiesInSameIndex()
        {
            ISession s = OpenSession();
            ITransaction tx = s.BeginTransaction();
            Drink d = new Drink();
            d.Name = "Water";
            Food f = new Food();
            f.Name = "Bread";
            s.Save(d);
            s.Save(f);
            tx.Commit();
            s.Close();

            s = OpenSession();
            tx = s.BeginTransaction();
            d = s.Get<Drink>(d.Id);
            d.Name = "Coke";
            f = s.Get<Food>(f.Id);
            f.Name = "Cake";
            try
            {
                tx.Commit();
            }
            catch // TODO: This Commit() succeeds, so I wonder why there is a try/catch
            {
                //Check for error logs from JDBCTransaction
            }
            s.Close();

            s = OpenSession();
            tx = s.BeginTransaction();
            d = s.Get<Drink>(d.Id);
            s.Delete(d);
            f = s.Get<Food>(f.Id);
            s.Delete(f);
            tx.Commit();
            s.Close();
        }
    }
}