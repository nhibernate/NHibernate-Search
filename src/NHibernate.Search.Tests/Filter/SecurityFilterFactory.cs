using Lucene.Net.Index;
using Lucene.Net.Search;
using NHibernate.Search.Attributes;
using NHibernate.Search.Filter;

namespace NHibernate.Search.Tests.Filter
{
    public class SecurityFilterFactory
    {
        private string login;

        public void SetLogin(string value)
        {
            login = value;
        }

        [Key]
        public FilterKey GetKey()
        {
            StandardFilterKey key = new StandardFilterKey();
            key.AddParameter(login);
            return key;
        }

        public Lucene.Net.Search.Filter GetFilter()
        {
            Lucene.Net.Search.Query query = new TermQuery(new Term("teacher", login));
            // TODO: Change this to QueryWrapperFilter when upgraded to Lucene 2.2
            return new CachingWrapperFilter(new QueryFilter(query));
        }
    }
}