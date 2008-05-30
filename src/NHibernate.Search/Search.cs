using System;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using NHibernate.Criterion;
using NHibernate.Search.Impl;

namespace NHibernate.Search
{
    public static class Search
    {
        public static IFullTextSession CreateFullTextSession(ISession session)
        {
            if (session is FullTextSessionImpl)
                return session as FullTextSessionImpl;
            else
                return new FullTextSessionImpl(session);
        }

        [Obsolete]
        public static ICriterion Query(Lucene.Net.Search.Query luceneQuery)
        {
            return new LuceneQueryExpression(luceneQuery);
        }

        [Obsolete]
        public static ICriterion Query(string luceneQuery)
        {
            QueryParser parser = new QueryParser("", new StandardAnalyzer());
            Lucene.Net.Search.Query query = parser.Parse(luceneQuery);
            return Query((Lucene.Net.Search.Query) query);
        }
    }
}