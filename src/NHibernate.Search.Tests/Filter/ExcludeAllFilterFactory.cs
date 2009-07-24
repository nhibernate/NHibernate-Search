using Lucene.Net.Search;
using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Filter
{
    public class ExcludeAllFilterFactory
    {
        [Factory]
        public Lucene.Net.Search.Filter GetFilter()
        {
            return new CachingWrapperFilter(new ExcludeAllFilter());
        }
    }
}