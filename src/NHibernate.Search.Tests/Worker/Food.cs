using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Worker
{
    [Indexed(Index="consumable")]
    public class Food
    {
        private int id;
        private string name;

        [DocumentId]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.Tokenized)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}
