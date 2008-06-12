using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Embedded
{
    [Indexed]
    public class Tower
    {
        [DocumentId]
        private long id;

        [Field(Index.Tokenized)]
        private string name;

        [IndexedEmbedded]
        private Address address;

        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual Address Address
        {
            get { return address; }
            set { address = value; }
        }
    }
}
