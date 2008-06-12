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
