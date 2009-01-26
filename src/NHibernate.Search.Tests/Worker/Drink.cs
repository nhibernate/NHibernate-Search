using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Worker
{
    [Indexed(Index="consumable")]
    public class Drink
    {
        private int id;
        private string name;

        [DocumentId]
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