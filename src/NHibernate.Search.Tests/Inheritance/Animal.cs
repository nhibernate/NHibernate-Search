using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Inheritance
{
    [Indexed]
    public class Animal
    {
        private int id;
        private string name;

        [DocumentId]
        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}