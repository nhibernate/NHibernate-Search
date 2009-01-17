using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Query
{
    [Indexed]
    public class Author
    {
        [DocumentId] 
        private int id;
        private string name;

        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.Tokenized)]
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}