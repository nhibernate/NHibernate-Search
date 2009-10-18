using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Shards
{
    [Indexed(Index = "Animal")]
    public class Animal
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