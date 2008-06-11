using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Embedded
{
    public class Author
    {
        [DocumentId]
        private int id;

        [Field(Index.Tokenized)]
        private string name;

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
    }
}
