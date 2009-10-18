using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Shards
{
    [Indexed]
    public class Furniture
    {
        [DocumentId]
        private int id;
        [Field]
        private string color;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Color
        {
            get { return color; }
            set { color = value; }
        }
    }
}