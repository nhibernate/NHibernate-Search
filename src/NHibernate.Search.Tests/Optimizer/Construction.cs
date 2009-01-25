using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Optimizer
{
    [Indexed]
    public class Construction
    {
        [DocumentId]
        private int id;

        [Field(Index.Tokenized)]
        private string name;

        [Field(Index.Tokenized)]
        private string address;

        public Construction() { }

        public Construction(string name, string address)
        {
            this.name = name;
            this.address = address;
        }

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

        public virtual string Address
        {
            get { return address; }
            set { address = value; }
        }
    }
}