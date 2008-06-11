using Iesi.Collections.Generic;
using NHibernate.Search.Attributes;
using System.Collections.Generic;

namespace NHibernate.Search.Tests.Embedded
{
    [Indexed]
    public class Product
    {
        private int id;
        private string name;
        private ISet<Author> authors = new HashedSet<Author>();
        private IDictionary<string, Order> orders = new Dictionary<string, Order>();

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public ISet<Author> Authors
        {
            get { return authors; }
            set { authors = value; }
        }

        public IDictionary<string, Order> Orders
        {
            get { return orders; }
            set { orders = value; }
        }
    }
}
