using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Search;
using NHibernate.Criterion;
using NHibernate.Impl;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Search.Util;
using NHibernate.SqlCommand;

namespace NHibernate.Search
{
    using Query;

    public class LuceneQueryExpression : InExpression
    {
        private readonly Lucene.Net.Search.Query luceneQuery;

        public LuceneQueryExpression(Lucene.Net.Search.Query luceneQuery)
            : base("id", new object[0])
        {
            this.luceneQuery = luceneQuery;
        }

        public override SqlString ToSqlString(ICriteria criteria, ICriteriaQuery criteriaQuery)
        {
            var type = GetCriteriaClass(criteria);
            var searchFactory = ContextHelper.GetSearchFactory(GetSession(criteria));
            var searcher = FullTextSearchHelper.BuildSearcher(searchFactory, out var types, type);
            if (searcher == null)
            {
                throw new SearchException("Could not find a searcher for class: " + type.FullName);
            }
            var query = FullTextSearchHelper.FilterQueryByClasses(types, luceneQuery);
            var results = searcher.Search(query, Int32.MaxValue);
            Values = results.ScoreDocs.Select(result => searcher.Doc(result.Doc))
                .Select(doc => DocumentBuilder.GetDocumentId(searchFactory, type, doc)).ToArray();
            return base.ToSqlString(criteria, criteriaQuery);
        }

        private static System.Type GetCriteriaClass(ICriteria criteria)
        {
            CriteriaImpl impl = criteria as CriteriaImpl;
            return impl != null ?
                impl.GetRootEntityTypeIfAvailable()
                : GetCriteriaClass(((CriteriaImpl.Subcriteria) criteria).Parent);
        }

        public ISession GetSession(ICriteria criteria)
        {
            CriteriaImpl impl = criteria as CriteriaImpl;
            return impl != null ? (ISession) impl.Session : this.GetSession(((CriteriaImpl.Subcriteria) criteria).Parent);
        }
    }
}