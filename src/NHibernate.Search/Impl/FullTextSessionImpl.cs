using System;
using System.Collections;
using System.Data;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Search.Backend;
using NHibernate.Search.Engine;
using NHibernate.Search.Query;
using NHibernate.Search.Util;
using NHibernate.Stat;
using NHibernate.Type;

namespace NHibernate.Search.Impl
{
    public class FullTextSessionImpl : IFullTextSession
    {
        private readonly ISession session;
        private readonly IEventSource eventSource;
        private readonly ISessionImplementor sessionImplementor;
        private ISearchFactoryImplementor searchFactory;

        public FullTextSessionImpl(ISession session)
        {
            this.session = session;
            this.eventSource = (IEventSource) session;
            this.sessionImplementor = (ISessionImplementor) session;
        }

        private ISearchFactory SearchFactory
        {
            get
            {
                if (searchFactory == null)
                    searchFactory = ContextHelper.GetSearchFactory(session);
                return searchFactory;
            }
        }

        private ISearchFactoryImplementor SearchFactoryImplementor
        {
            get
            {
                if (searchFactory == null)
                    searchFactory = ContextHelper.GetSearchFactory(session);
                return searchFactory;
            }
        }

        #region Delegating to Inner Session

        public void Flush()
        {
            session.Flush();
        }

        public IDbConnection Disconnect()
        {
            return session.Disconnect();
        }

        public void Reconnect()
        {
            session.Reconnect();
        }

        public void Reconnect(IDbConnection connection)
        {
            session.Reconnect(connection);
        }

        public IDbConnection Close()
        {
            return session.Close();
        }

        public void CancelQuery()
        {
            session.CancelQuery();
        }

        public bool IsDirty()
        {
            return session.IsDirty();
        }

        public object GetIdentifier(object obj)
        {
            return session.GetIdentifier(obj);
        }

        public bool Contains(object obj)
        {
            return session.Contains(obj);
        }

        public void Evict(object obj)
        {
            session.Evict(obj);
        }

        public object Load(System.Type theType, object id, LockMode lockMode)
        {
            return session.Load(theType, id, lockMode);
        }

#if !NHIBERNATE20
    	public object Load(string entityName, object id, LockMode lockMode)
		{
			return session.Load(entityName, id, lockMode);
    	}
#endif

    	public object Load(System.Type theType, object id)
        {
            return session.Load(theType, id);
        }

        public T Load<T>(object id, LockMode lockMode)
        {
            return session.Load<T>(id, lockMode);
        }

        public T Load<T>(object id)
        {
            return session.Load<T>(id);
        }

#if !NHIBERNATE20
        public object Load(string entityName, object id)
		{
    		return session.Load(entityName, id);
    	}
#endif

    	public void Load(object obj, object id)
        {
            session.Load(obj, id);
        }

        public void Replicate(object obj, ReplicationMode replicationMode)
        {
            session.Replicate(obj, replicationMode);
        }

        public ISessionStatistics Statistics
        {
            get { return session.Statistics; }
        }

#if !NHIBERNATE20
        public EntityMode ActiveEntityMode
    	{
    		get { return session.ActiveEntityMode; }
    	}
#endif

    	public FlushMode FlushMode
        {
            get { return session.FlushMode; }
            set { session.FlushMode = value; }
        }

        public CacheMode CacheMode
        {
            get { return session.CacheMode; }
            set { session.CacheMode = value; }
        }

        public ISessionFactory SessionFactory
        {
            get { return session.SessionFactory; }
        }

        public IDbConnection Connection
        {
            get { return session.Connection; }
        }

        public bool IsOpen
        {
            get { return session.IsOpen; }
        }

        public bool IsConnected
        {
            get { return session.IsConnected; }
        }

        public ITransaction Transaction
        {
            get { return session.Transaction; }
        }

        public ISession SetBatchSize(int batchSize)
        {
            return session.SetBatchSize(batchSize);
        }

        public ISessionImplementor GetSessionImplementation()
        {
            return session.GetSessionImplementation();
        }

        public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
        {
            session.Replicate(entityName, obj, replicationMode);
        }

        public object Save(object obj)
        {
            return session.Save(obj);
        }

        public void Save(object obj, object id)
        {
            session.Save(obj, id);
        }

        public object Save(string entityName, object obj)
        {
            return session.Save(entityName, obj);
        }

        public void SaveOrUpdate(object obj)
        {
            session.SaveOrUpdate(obj);
        }

        public void SaveOrUpdate(string entityName, object obj)
        {
            session.SaveOrUpdate(entityName, obj);
        }

        public void Update(object obj)
        {
            session.Update(obj);
        }

        public void Update(object obj, object id)
        {
            session.Update(obj, id);
        }

        public void Update(string entityName, object obj)
        {
            session.Update(entityName, obj);
        }

        public object Merge(object obj)
        {
            return session.Merge(obj);
        }

        public object Merge(string entityName, object obj)
        {
            return session.Merge(entityName, obj);
        }

        public void Persist(object obj)
        {
            session.Persist(obj);
        }

        public void Persist(string entityName, object obj)
        {
            session.Persist(entityName, obj);
        }

        public object SaveOrUpdateCopy(object obj)
        {
            return session.SaveOrUpdateCopy(obj);
        }

        public object SaveOrUpdateCopy(object obj, object id)
        {
            return session.SaveOrUpdateCopy(obj, id);
        }

        public void Delete(object obj)
        {
            session.Delete(obj);
        }

#if !NHIBERNATE20
        public void Delete(string entityName, object obj)
    	{
    		session.Delete(entityName, obj);
    	}
#endif

    	public IList Find(string query)
        {
#pragma warning disable 618,612
            return session.Find(query);
#pragma warning restore 618,612
        }

        public IList Find(string query, object value, IType type)
        {
#pragma warning disable 618,612
            return session.Find(query, value, type);
#pragma warning restore 618,612
        }

        public IList Find(string query, object[] values, IType[] types)
        {
#pragma warning disable 618,612
            return session.Find(query, values, types);
#pragma warning restore 618,612
        }

        public IEnumerable Enumerable(string query)
        {
#pragma warning disable 618,612
            return session.Enumerable(query);
#pragma warning restore 618,612
        }

        public IEnumerable Enumerable(string query, object value, IType type)
        {
#pragma warning disable 618,612
            return session.Enumerable(query, value, type);
#pragma warning restore 618,612
        }

        public IEnumerable Enumerable(string query, object[] values, IType[] types)
        {
#pragma warning disable 618,612
            return session.Enumerable(query, values, types);
#pragma warning restore 618,612
        }

        public ICollection Filter(object collection, string filter)
        {
#pragma warning disable 618,612
            return session.Filter(collection, filter);
#pragma warning restore 618,612
        }

        public ICollection Filter(object collection, string filter, object value, IType type)
        {
#pragma warning disable 618,612
            return session.Filter(collection, filter, value, type);
#pragma warning restore 618,612
        }

        public ICollection Filter(object collection, string filter, object[] values, IType[] types)
        {
#pragma warning disable 618,612
            return session.Filter(collection, filter, values, types);
#pragma warning restore 618,612
        }

        public int Delete(string query)
        {
            return session.Delete(query);
        }

        public int Delete(string query, object value, IType type)
        {
            return session.Delete(query, value, type);
        }

        public int Delete(string query, object[] values, IType[] types)
        {
            return session.Delete(query, values, types);
        }

        public void Lock(object obj, LockMode lockMode)
        {
            session.Lock(obj, lockMode);
        }

        public void Lock(string entityName, object obj, LockMode lockMode)
        {
            session.Lock(entityName, obj, lockMode);
        }

        public void Refresh(object obj)
        {
            session.Refresh(obj);
        }

        public void Refresh(object obj, LockMode lockMode)
        {
            session.Refresh(obj, lockMode);
        }

        public LockMode GetCurrentLockMode(object obj)
        {
            return session.GetCurrentLockMode(obj);
        }

        public ITransaction BeginTransaction()
        {
            return session.BeginTransaction();
        }

        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return session.BeginTransaction(isolationLevel);
        }

        public ICriteria CreateCriteria(System.Type persistentClass)
        {
            return session.CreateCriteria(persistentClass);
        }

        public ICriteria CreateCriteria(System.Type persistentClass, string alias)
        {
            return session.CreateCriteria(persistentClass, alias);
        }

#if !NHIBERNATE20
        public ICriteria CreateCriteria(string entityName)
		{
			return session.CreateCriteria(entityName);
    	}

    	public ICriteria CreateCriteria(string entityName, string alias)
		{
			return session.CreateCriteria(entityName, alias);
    	}
#endif

    	public IQuery CreateQuery(string queryString)
        {
            return session.CreateQuery(queryString);
        }

        public IQuery CreateFilter(object collection, string queryString)
        {
            return session.CreateFilter(collection, queryString);
        }

        public IQuery GetNamedQuery(string queryName)
        {
            return session.GetNamedQuery(queryName);
        }

        public IQuery CreateSQLQuery(string sql, string returnAlias, System.Type returnClass)
        {
#pragma warning disable 618,612
            return session.CreateSQLQuery(sql, returnAlias, returnClass);
#pragma warning restore 618,612
        }

        public IQuery CreateSQLQuery(string sql, string[] returnAliases, System.Type[] returnClasses)
        {
#pragma warning disable 618,612
            return session.CreateSQLQuery(sql, returnAliases, returnClasses);
#pragma warning restore 618,612
        }

        public ISQLQuery CreateSQLQuery(string queryString)
        {
            return session.CreateSQLQuery(queryString);
        }

        public void Clear()
        {
            session.Clear();
        }

        public object Get(System.Type clazz, object id)
        {
            return session.Get(clazz, id);
        }

        public object Get(System.Type clazz, object id, LockMode lockMode)
        {
            return session.Get(clazz, id, lockMode);
        }

        public object Get(string entityName, object id)
        {
            return session.Get(entityName, id);
        }

        public T Get<T>(object id)
        {
            return session.Get<T>(id);
        }

        public T Get<T>(object id, LockMode lockMode)
        {
            return session.Get<T>(id, lockMode);
        }

        public String GetEntityName(object obj)
        {
            return session.GetEntityName(obj);
        }

        public IFilter EnableFilter(string filterName)
        {
            return session.EnableFilter(filterName);
        }

        public IFilter GetEnabledFilter(string filterName)
        {
            return session.GetEnabledFilter(filterName);
        }

        public void DisableFilter(string filterName)
        {
            session.DisableFilter(filterName);
        }

        public IMultiQuery CreateMultiQuery()
        {
            return session.CreateMultiQuery();
        }

        public IMultiCriteria CreateMultiCriteria()
        {
            return session.CreateMultiCriteria();
        }

        public ISession GetSession(EntityMode entityMode)
        {
            return session.GetSession(entityMode);
        }

        #endregion

        #region IFullTextSession Members

        public IFullTextQuery CreateFullTextQuery<TEntity>(string defaultField, string queryString)
        {
            QueryParser queryParser = new QueryParser(defaultField, new StandardAnalyzer());
            Lucene.Net.Search.Query query = queryParser.Parse(queryString);
            return CreateFullTextQuery(query, typeof(TEntity));
        }

        public IFullTextQuery CreateFullTextQuery<TEntity>(string queryString)
        {
            QueryParser queryParser = new QueryParser(string.Empty, new StandardAnalyzer());
            Lucene.Net.Search.Query query = queryParser.Parse(queryString);
            return CreateFullTextQuery(query, typeof(TEntity));
        }

        /// <summary>
        /// Execute a Lucene query and retrieve managed objects of type entities (or their indexed subclasses
        /// If entities is empty, include all indexed entities
        /// </summary>
        /// <param name="luceneQuery"></param>
        /// <param name="entities">entities must be immutable for the lifetime of the query object</param>
        /// <returns></returns>
        public IFullTextQuery CreateFullTextQuery(Lucene.Net.Search.Query luceneQuery, params System.Type[] entities)
        {
            return new FullTextQueryImpl(luceneQuery, entities, session, null);
        }

        /// <summary>
        /// (re)index an entity.
        /// Non indexable entities are ignored
        /// The entity must be associated with the session
        /// </summary>
        /// <param name="entity">he neity to index - must not be null</param>
        /// <returns></returns>
        public IFullTextSession Index(object entity)
        {
            if (entity == null) return this;

            System.Type clazz = NHibernateUtil.GetClass(entity);
            // TODO: Cache that at the FTSession level
            ISearchFactoryImplementor searchFactoryImplementor = SearchFactoryImplementor;
            // not strictly necesary but a small optmization
            DocumentBuilder builder = searchFactoryImplementor.DocumentBuilders[clazz];
            if (builder != null)
            {
                object id = session.GetIdentifier(entity);
                Work work = new Work(entity, id, WorkType.Index);
                searchFactoryImplementor.Worker.PerformWork(work, eventSource);
            }
            return this;
        }

        public void PurgeAll(System.Type clazz)
        {
            Purge(clazz, null);
        }

        public void Purge(System.Type clazz, object id)
        {
            if (clazz == null)
            {
                return;
            }

            ISearchFactoryImplementor searchFactoryImplementor = SearchFactoryImplementor;
            // TODO: Cache that at the FTSession level
            // not strictly necesary but a small optmization
            DocumentBuilder builder = searchFactoryImplementor.DocumentBuilders[clazz];
            if (builder != null)
            {
                // TODO: Check to see this entity type is indexed
                WorkType workType = id == null ? WorkType.PurgeAll : WorkType.Purge;
                Work work = new Work(clazz, id, workType);
                searchFactoryImplementor.Worker.PerformWork(work, eventSource);
            }
        }

        public void Dispose()
        {
            session.Dispose();
        }

        #endregion
    }
}