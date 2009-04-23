using System;
using System.Collections;
using log4net;
using NHibernate.Criterion;

namespace NHibernate.Search.Engine
{
    public class QueryLoader : ILoader
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(QueryLoader));
        private static readonly IList EMPTY_LIST = new ArrayList();
        private const int MAX_IN_CLAUSE = 500;

        private ISearchFactoryImplementor searchFactoryImplementor;
        private ISession session;
        private System.Type entityType;
        private ICriteria criteria;

        public System.Type EntityType
        {
            get { return entityType; }
            set { entityType = value; }
        }

        public ICriteria Criteria
        {
            get { return criteria; }
            set { criteria = value; }
        }

        #region ILoader Members

        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.session = session;
            this.searchFactoryImplementor = searchFactoryImplementor;
        }

        public object Load(EntityInfo entityInfo)
        {
            object maybeProxy = session.Get(entityInfo.Clazz, entityInfo.Id);
            // TODO: Initialize call and error trapping

            return maybeProxy;
        }

        public IList Load(params EntityInfo[] entityInfos)
        {
            int maxResults = entityInfos.Length;
            if (maxResults == 0) return EMPTY_LIST;
            if (entityType == null) throw new NotSupportedException("EntityType not defined");
            if (criteria == null) criteria = session.CreateCriteria(entityType);

            DocumentBuilder builder = searchFactoryImplementor.DocumentBuilders[entityType];
            string idName = builder.GetIdKeywordName();
            int loop = maxResults/MAX_IN_CLAUSE;
            bool exact = maxResults % MAX_IN_CLAUSE == 0;
            if (!exact) loop++;

            Disjunction disjunction = Restrictions.Disjunction();
            for (int index = 0; index < loop; index++)
            {
                int max = index * MAX_IN_CLAUSE + MAX_IN_CLAUSE <= maxResults ?
					index * MAX_IN_CLAUSE + MAX_IN_CLAUSE :
					maxResults;
                IList ids = new ArrayList(max - index*MAX_IN_CLAUSE);
                for (int entityInfoIndex = index * MAX_IN_CLAUSE; entityInfoIndex < max; entityInfoIndex++)
                {
                    ids.Add(entityInfos[entityInfoIndex].Id);
                }
                disjunction.Add(Restrictions.In(idName, ids));
            }
            criteria.Add(disjunction);
            criteria.List(); // Load all objects

            // Mandatory to keep the same ordering // TODO: Would it be faster to keep the list and then sort it?
            IList result = new ArrayList(entityInfos.Length);
            foreach (EntityInfo entityInfo in entityInfos)
            {
                object element = session.Load(entityInfo.Clazz, entityInfo.Id);
                if (NHibernateUtil.IsInitialized(element))
                {
                    //all existing elements should have been loaded by the query,
                    //the other ones are missing ones
                    result.Add(element);
                }
                else
                    log.Warn("Lucene index contains info about entity " + entityInfo.Clazz.Name + "#" + entityInfo.Id + " which wasn't found in the database. Rebuild the index.");
            }

            return result;
        }

        #endregion
    }
}