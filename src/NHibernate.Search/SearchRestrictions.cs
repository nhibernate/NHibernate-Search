using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using NHibernate.Criterion;

namespace NHibernate.Search
{
    public static class SearchRestrictions
    {
        public static ICriterion Query(Lucene.Net.Search.Query luceneQuery)
        {
            return new LuceneQueryExpression(luceneQuery);
        }

        public static ICriterion Query(string luceneQuery)
        {
            QueryParser parser = new QueryParser(string.Empty, new StandardAnalyzer());
            return Query(parser.Parse(luceneQuery));
        }

        public static ICriterion Query(string defaultField, string luceneQuery)
        {
            QueryParser parser = new QueryParser(defaultField, new StandardAnalyzer());
            return Query(parser.Parse(luceneQuery));
        }
    }
}