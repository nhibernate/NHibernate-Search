using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Util;
using NHibernate.Criterion;

namespace NHibernate.Search.Engine
{
    public class QueryLoader : ILoader, IAsyncLoader
    {
        private static readonly INHibernateLogger log = NHibernateLogger.For(typeof(QueryLoader));
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

        /// <inheritdoc />
        public async ValueTask<Object> LoadAsync(EntityInfo entityInfo, CancellationToken token)
        {
            object maybeProxy = session.GetAsync(entityInfo.Clazz, entityInfo.Id, token);
            // TODO: Initialize call and error trapping

            return maybeProxy;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<(EntityInfo EntityInfo, Object Entity)> LoadAsync(IReadOnlyList<EntityInfo> entityInfos, [EnumeratorCancellation] CancellationToken token)
        {
            int maxResults = entityInfos.Count;
            if (maxResults == 0) yield break;
            if (entityType == null) throw new NotSupportedException("EntityType not defined");
            if (criteria == null) criteria = session.CreateCriteria(entityType);

            DocumentBuilder builder = searchFactoryImplementor.DocumentBuilders[entityType];
            string idName = builder.IdentifierName;
            int loop = maxResults / MAX_IN_CLAUSE;
            bool exact = maxResults % MAX_IN_CLAUSE == 0;
            if (!exact) loop++;

            Disjunction disjunction = Restrictions.Disjunction();
            for (int index = 0; index < loop; index++)
            {
                int max = index * MAX_IN_CLAUSE + MAX_IN_CLAUSE <= maxResults ?
                    index * MAX_IN_CLAUSE + MAX_IN_CLAUSE :
                    maxResults;
                IList ids = new ArrayList(max - index * MAX_IN_CLAUSE);
                for (int entityInfoIndex = index * MAX_IN_CLAUSE; entityInfoIndex < max; entityInfoIndex++)
                {
                    ids.Add(entityInfos[entityInfoIndex].Id);
                }
                disjunction.Add(Restrictions.In(idName, ids));
            }
            criteria.Add(disjunction);
            IList<Object> entities = await criteria.ListAsync<Object>(token); // Load all objects, though the sort will be incorrect

            PropertyInfo idProperty = entityType.GetProperty(idName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Object GetEntityId(Object entity) => idProperty.GetValue(entity);

            IDictionary<Object, Object> entitySortMap =
                entities.ToDictionary(GetEntityId, e => e);

            foreach (EntityInfo entityInfo in entityInfos)
            {
                var entity = entitySortMap[entityInfo.Id];
                if (NHibernateUtil.IsInitialized(entity))
                {
                    //all existing elements should have been loaded by the query,
                    //the other ones are missing ones
                    yield return (entityInfo, entity);
                }
                else
                    log.Warn("Lucene index contains info about entity {0} #{1} which wasn't found in the database. Rebuild the index.", entityInfo.Clazz.Name, entityInfo.Id);
            }
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
            string idName = builder.IdentifierName;
            int loop = maxResults / MAX_IN_CLAUSE;
            bool exact = maxResults % MAX_IN_CLAUSE == 0;
            if (!exact) loop++;

            Disjunction disjunction = Restrictions.Disjunction();
            for (int index = 0; index < loop; index++)
            {
                int max = index * MAX_IN_CLAUSE + MAX_IN_CLAUSE <= maxResults ?
                    index * MAX_IN_CLAUSE + MAX_IN_CLAUSE :
                    maxResults;
                IList ids = new ArrayList(max - index * MAX_IN_CLAUSE);
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