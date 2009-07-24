using Lucene.Net.Index;
using Lucene.Net.Search;
using NHibernate.Search.Attributes;
using NHibernate.Search.Filter;

namespace NHibernate.Search.Tests.Filter
{
    public class SecurityFilterFactory
    {
        private string login;

        [FilterParameter]
        public string Login
        {
            get { return login; }
            set { login = value; }
        }

        [Key]
        public FilterKey GetKey()
        {
            StandardFilterKey key = new StandardFilterKey();
            key.AddParameter(login);
            return key;
        }

        [Factory]
        public Lucene.Net.Search.Filter GetFilter()
        {
            Lucene.Net.Search.Query query = new TermQuery(new Term("teacher", login));
            return new CachingWrapperFilter(new QueryWrapperFilter(query));
        }
    }
}