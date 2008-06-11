using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Embedded
{
    public class Owner : Person
    {
        [Field(Index.Tokenized)]
        private string name;

        [IndexedEmbedded] // play the lunatic user
        private Address address;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public Address Address
        {
            get { return address; }
            set { address = value; }
        }
    }
}
