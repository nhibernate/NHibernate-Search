using System.Collections.Generic;
using Iesi.Collections.Generic;
using NHibernate.Search.Attributes;


namespace NHibernate.Search.Tests.Embedded
{
    [Indexed]
    public class Product
    {
        [DocumentId]
        private int id;
        [Field(Index.Tokenized)]
        private string name;
        [IndexedEmbedded]
        private ISet<Author> authors = new HashSet<Author>();
        [IndexedEmbedded]
        private IDictionary<string, Order> orders = new Dictionary<string, Order>();

        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual ISet<Author> Authors
        {
            get { return authors; }
            set { authors = value; }
        }

        public virtual IDictionary<string, Order> Orders
        {
            get { return orders; }
            set { orders = value; }
        }
    }
}
