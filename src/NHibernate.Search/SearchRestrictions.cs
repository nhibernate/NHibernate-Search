using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NHibernate.Criterion;

namespace NHibernate.Search
{
    using System;

    [Obsolete("Use SetCriteriaQuery against IFullTextSession")]
    public static class SearchRestrictions
    {
        public static ICriterion Query(Lucene.Net.Search.Query luceneQuery)
        {
            return new LuceneQueryExpression(luceneQuery);
        }

        public static ICriterion Query(string luceneQuery)
        {
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, string.Empty, new StandardAnalyzer(LuceneVersion.LUCENE_48));
            return Query(parser.Parse(luceneQuery));
        }

        public static ICriterion Query(string defaultField, string luceneQuery)
        {
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, defaultField, new StandardAnalyzer(LuceneVersion.LUCENE_48));
            return Query(parser.Parse(luceneQuery));
        }
    }
}