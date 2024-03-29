using System.Collections.Generic;
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
            System.Type type = GetCriteriaClass(criteria);
            ISearchFactoryImplementor searchFactory = ContextHelper.GetSearchFactory(GetSession(criteria));
            ISet<System.Type> types;
            IndexSearcher searcher = FullTextSearchHelper.BuildSearcher(searchFactory, out types, type);
            if (searcher == null)
                throw new SearchException("Could not find a searcher for class: " + type.FullName);
            Lucene.Net.Search.Query query = FullTextSearchHelper.FilterQueryByClasses(types, luceneQuery);
            TopDocs topDocs = searcher.Search(query, int.MaxValue);
            List<object> ids = new List<object>();
            foreach (var scoreDoc in topDocs.ScoreDocs)
            {
                object id = DocumentBuilder.GetDocumentId(searchFactory, type, searcher.Doc(scoreDoc.Doc));
                ids.Add(id);
            }

            base.Values = ids.ToArray();
            return base.ToSqlString(criteria, criteriaQuery);
        }

        private static System.Type GetCriteriaClass(ICriteria criteria)
        {
            CriteriaImpl impl = criteria as CriteriaImpl;
            return impl != null ?
                impl.GetRootEntityTypeIfAvailable()
                : GetCriteriaClass(((CriteriaImpl.Subcriteria)criteria).Parent);
        }

        public ISession GetSession(ICriteria criteria)
        {
            CriteriaImpl impl = criteria as CriteriaImpl;
            return impl != null ? (ISession)impl.Session : this.GetSession(((CriteriaImpl.Subcriteria)criteria).Parent);
        }
    }
}