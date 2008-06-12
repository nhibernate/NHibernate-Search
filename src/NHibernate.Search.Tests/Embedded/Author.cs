using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Embedded
{
    public class Author
    {
        [DocumentId]
        private int id;

        [Field(Index.Tokenized)]
        private string name;

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
    }
}
