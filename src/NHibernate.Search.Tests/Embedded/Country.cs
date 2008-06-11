using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Embedded
{
    [Indexed]
    public class Country
    {
        [DocumentId]
        private int id;

        [Field]
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
