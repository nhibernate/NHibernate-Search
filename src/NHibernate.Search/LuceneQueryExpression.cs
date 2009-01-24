using System.Collections.Generic;
using Iesi.Collections.Generic;
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

        public override SqlString ToSqlString(ICriteria criteria, ICriteriaQuery criteriaQuery,
                                              IDictionary<string, IFilter> enabledFilters)
        {
            System.Type type = GetCriteriaClass(criteria);
            ISearchFactoryImplementor searchFactory = ContextHelper.GetSearchFactory(GetSession(criteria));
            ISet<System.Type> types;
            IndexSearcher searcher = FullTextSearchHelper.BuildSearcher(searchFactory, out types, type);
            if (searcher == null)
                throw new SearchException("Could not find a searcher for class: " + type.FullName);
            Lucene.Net.Search.Query query = FullTextSearchHelper.FilterQueryByClasses(types, luceneQuery);
            Hits hits = searcher.Search(query);
            List<object> ids = new List<object>();
            for (int i = 0; i < hits.Length(); i++)
            {
                object id = DocumentBuilder.GetDocumentId(searchFactory, type, hits.Doc(i));
                ids.Add(id);
            }
            base.Values = ids.ToArray();
            return base.ToSqlString(criteria, criteriaQuery, enabledFilters);
        }

        private static System.Type GetCriteriaClass(ICriteria criteria)
        {
            CriteriaImpl impl = criteria as CriteriaImpl;
            return impl != null ? impl.CriteriaClass : GetCriteriaClass(((CriteriaImpl.Subcriteria) criteria).Parent);
        }

        public ISession GetSession(ICriteria criteria)
        {
            CriteriaImpl impl = criteria as CriteriaImpl;
            return impl != null ? impl.Session.GetSession() : this.GetSession(((CriteriaImpl.Subcriteria) criteria).Parent);
        }
    }
}