using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Filter
{
    public class Soap
    {
        [DocumentId] private int id;
        private string perfume;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Perfume
        {
            get { return perfume; }
            set { perfume = value; }
        }
    }
}