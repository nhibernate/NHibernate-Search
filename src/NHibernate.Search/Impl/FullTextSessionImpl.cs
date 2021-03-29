using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Impl;
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
        private DbConnection _connection;

        public FullTextSessionImpl(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.eventSource = (IEventSource)session;
            this.sessionImplementor = (ISessionImplementor)session;
        }

        public ISearchFactory SearchFactory => searchFactory ??= ContextHelper.GetSearchFactory(session);

        private ISearchFactoryImplementor SearchFactoryImplementor => searchFactory ??= ContextHelper.GetSearchFactory(session);

        #region Delegating to Inner Session

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken = default) => session.FlushAsync(cancellationToken);

        /// <inheritdoc />
        public Task<Boolean> IsDirtyAsync(CancellationToken cancellationToken = default) => session.IsDirtyAsync(cancellationToken);

        /// <inheritdoc />
        public Task EvictAsync(Object obj, CancellationToken cancellationToken = default) =>
            session.EvictAsync(obj, cancellationToken);

        /// <inheritdoc />
        public Task<Object> LoadAsync(System.Type theType, Object id, LockMode lockMode,
            CancellationToken cancellationToken = default) =>
            session.LoadAsync(theType, id, lockMode, cancellationToken);

        /// <inheritdoc />
        public Task<Object> LoadAsync(String entityName, Object id, LockMode lockMode,
            CancellationToken cancellationToken = default) =>
            session.LoadAsync(entityName, id, lockMode, cancellationToken);

        /// <inheritdoc />
        public Task<Object> LoadAsync(System.Type theType, Object id, CancellationToken cancellationToken = default) =>
            session.LoadAsync(theType, id, cancellationToken);

        /// <inheritdoc />
        public Task<T> LoadAsync<T>(Object id, LockMode lockMode, CancellationToken cancellationToken = default) =>
            session.LoadAsync<T>(id, lockMode, cancellationToken);

        /// <inheritdoc />
        public Task<T> LoadAsync<T>(Object id, CancellationToken cancellationToken = default) =>
            session.LoadAsync<T>(id, cancellationToken);

        /// <inheritdoc />
        public Task<Object> LoadAsync(String entityName, Object id, CancellationToken cancellationToken = default) =>
            session.LoadAsync(entityName, id, cancellationToken);

        /// <inheritdoc />
        public Task LoadAsync(Object obj, Object id, CancellationToken cancellationToken = default) =>
            session.LoadAsync(obj, id, cancellationToken);

        /// <inheritdoc />
        public Task ReplicateAsync(Object obj, ReplicationMode replicationMode,
            CancellationToken cancellationToken = default) =>
            session.ReplicateAsync(obj, replicationMode, cancellationToken);

        /// <inheritdoc />
        public Task ReplicateAsync(String entityName, Object obj, ReplicationMode replicationMode,
            CancellationToken cancellationToken = default) =>
            session.ReplicateAsync(entityName, obj, replicationMode, cancellationToken);

        /// <inheritdoc />
        public Task<Object> SaveAsync(Object obj, CancellationToken cancellationToken = default) =>
            session.SaveAsync(obj, cancellationToken);

        /// <inheritdoc />
        public Task SaveAsync(Object obj, Object id, CancellationToken cancellationToken = default) =>
            session.SaveAsync(obj, id, cancellationToken);

        /// <inheritdoc />
        public Task<Object> SaveAsync(String entityName, Object obj, CancellationToken cancellationToken = default) =>
            session.SaveAsync(entityName, obj, cancellationToken);

        /// <inheritdoc />
        public Task SaveAsync(String entityName, Object obj, Object id, CancellationToken cancellationToken = default) =>
            session.SaveAsync(entityName, obj, id, cancellationToken);

        /// <inheritdoc />
        public Task SaveOrUpdateAsync(Object obj, CancellationToken cancellationToken = default) =>
            session.SaveOrUpdateAsync(obj, cancellationToken);

        /// <inheritdoc />
        public Task SaveOrUpdateAsync(String entityName, Object obj, CancellationToken cancellationToken = default) =>
            session.SaveOrUpdateAsync(entityName, obj, cancellationToken);

        /// <inheritdoc />
        public Task SaveOrUpdateAsync(String entityName, Object obj, Object id,
            CancellationToken cancellationToken = default) =>
            session.SaveOrUpdateAsync(entityName, obj, id, cancellationToken);

        /// <inheritdoc />
        public Task UpdateAsync(Object obj, CancellationToken cancellationToken = default) =>
            session.UpdateAsync(obj, cancellationToken);

        /// <inheritdoc />
        public Task UpdateAsync(Object obj, Object id, CancellationToken cancellationToken = default) =>
            session.UpdateAsync(obj, id, cancellationToken);

        /// <inheritdoc />
        public Task UpdateAsync(String entityName, Object obj, CancellationToken cancellationToken = default) =>
            session.UpdateAsync(entityName, obj, cancellationToken);

        /// <inheritdoc />
        public Task UpdateAsync(String entityName, Object obj, Object id,
            CancellationToken cancellationToken = default) =>
            session.UpdateAsync(entityName, obj, id, cancellationToken);

        /// <inheritdoc />
        public Task<Object> MergeAsync(Object obj, CancellationToken cancellationToken = default) =>
            session.MergeAsync(obj, cancellationToken);

        /// <inheritdoc />
        public Task<Object> MergeAsync(String entityName, Object obj, CancellationToken cancellationToken = default) =>
            session.MergeAsync(entityName, obj, cancellationToken);

        /// <inheritdoc />
        public Task<T> MergeAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class =>
            session.MergeAsync<T>(entity, cancellationToken);

        /// <inheritdoc />
        public Task<T> MergeAsync<T>(String entityName, T entity, CancellationToken cancellationToken = default) where T : class =>
            session.MergeAsync<T>(entityName, entity, cancellationToken);

        /// <inheritdoc />
        public Task PersistAsync(Object obj, CancellationToken cancellationToken = default) =>
            session.PersistAsync(obj, cancellationToken);

        /// <inheritdoc />
        public Task PersistAsync(String entityName, Object obj, CancellationToken cancellationToken = default) =>
            session.PersistAsync(entityName, obj, cancellationToken);

        /// <inheritdoc />
        public Task DeleteAsync(Object obj, CancellationToken cancellationToken = default) =>
            session.DeleteAsync(obj, cancellationToken);

        /// <inheritdoc />
        public Task DeleteAsync(String entityName, Object obj, CancellationToken cancellationToken = default) =>
            session.DeleteAsync(entityName, obj, cancellationToken);

        /// <inheritdoc />
        public Task<Int32> DeleteAsync(String query, CancellationToken cancellationToken = default) =>
            session.DeleteAsync(query, cancellationToken);

        /// <inheritdoc />
        public Task<Int32> DeleteAsync(String query, Object value, IType type, CancellationToken cancellationToken = default) =>
            session.DeleteAsync(query, value, type, cancellationToken);

        /// <inheritdoc />
        public Task<Int32> DeleteAsync(String query, Object[] values, IType[] types,
            CancellationToken cancellationToken = default) =>
            session.DeleteAsync(query, values, types, cancellationToken);

        /// <inheritdoc />
        public Task LockAsync(Object obj, LockMode lockMode, CancellationToken cancellationToken = default) =>
            session.LockAsync(obj, lockMode, cancellationToken);

        /// <inheritdoc />
        public Task LockAsync(String entityName, Object obj, LockMode lockMode,
            CancellationToken cancellationToken = default) =>
            session.LockAsync(entityName, obj, lockMode, cancellationToken);

        /// <inheritdoc />
        public Task RefreshAsync(Object obj, CancellationToken cancellationToken = default) =>
            session.RefreshAsync(obj, cancellationToken);

        /// <inheritdoc />
        public Task RefreshAsync(Object obj, LockMode lockMode, CancellationToken cancellationToken = default) =>
            session.RefreshAsync(obj, lockMode, cancellationToken);

        /// <inheritdoc />
        public Task<IQuery> CreateFilterAsync(Object collection, String queryString,
            CancellationToken cancellationToken = default) =>
            session.CreateFilterAsync(collection, queryString, cancellationToken);

        /// <inheritdoc />
        public Task<Object> GetAsync(System.Type clazz, Object id, CancellationToken cancellationToken = default) =>
            session.GetAsync(clazz, id, cancellationToken);

        /// <inheritdoc />
        public Task<Object> GetAsync(System.Type clazz, Object id, LockMode lockMode, CancellationToken cancellationToken = default) =>
            session.GetAsync(clazz, id, lockMode, cancellationToken);

        /// <inheritdoc />
        public Task<Object> GetAsync(String entityName, Object id, CancellationToken cancellationToken = default) =>
            session.GetAsync(entityName, id, cancellationToken);

        /// <inheritdoc />
        public Task<T> GetAsync<T>(Object id, CancellationToken cancellationToken = default) =>
            session.GetAsync<T>(id, cancellationToken);

        /// <inheritdoc />
        public Task<T> GetAsync<T>(Object id, LockMode lockMode, CancellationToken cancellationToken = default) =>
            session.GetAsync<T>(id, lockMode, cancellationToken);

        /// <inheritdoc />
        public Task<String> GetEntityNameAsync(Object obj, CancellationToken cancellationToken = default) =>
            session.GetEntityNameAsync(obj, cancellationToken);

        /// <inheritdoc />
        public ISharedSessionBuilder SessionWithOptions()
        {
            // TODO: not sure how to return a builder on this session, instead of leaking out the wrapped session
            throw new NotImplementedException();
        }

        public void Flush()
        {
            session.Flush();
        }

        public DbConnection Disconnect()
        {
            return session.Disconnect();
        }

        public void Reconnect()
        {
            session.Reconnect();
        }
        
        public void Reconnect(DbConnection connection)
        {
            session.Reconnect(connection);
        }

        public DbConnection Close()
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

        public bool IsReadOnly(object entityOrProxy)
        {
            return session.IsReadOnly(entityOrProxy);
        }

        public void SetReadOnly(object entityOrProxy, bool readOnly)
        {
            session.SetReadOnly(entityOrProxy, readOnly);
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

        public object Load(string entityName, object id, LockMode lockMode)
        {
            return session.Load(entityName, id, lockMode);
        }

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

        public object Load(string entityName, object id)
        {
            return session.Load(entityName, id);
        }

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

        /// <inheritdoc />
        public IQueryable<T> Query<T>(String entityName)
        {
            throw new NotImplementedException();
        }

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
        
        public DbConnection Connection
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

        public bool DefaultReadOnly
        {
            get { return session.DefaultReadOnly; }
            set { session.DefaultReadOnly = value; }
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

        /// <inheritdoc />
        public void Save(String entityName, Object obj, Object id)
        {
            session.Save(entityName, obj, id);
        }

        public void SaveOrUpdate(object obj)
        {
            session.SaveOrUpdate(obj);
        }

        public void SaveOrUpdate(string entityName, object obj)
        {
            session.SaveOrUpdate(entityName, obj);
        }

        /// <inheritdoc />
        public void SaveOrUpdate(String entityName, Object obj, Object id)
        {
            session.SaveOrUpdate(entityName, obj, id);
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

        /// <inheritdoc />
        public void Update(String entityName, Object obj, Object id)
        {
            session.Update(entityName, obj, id);
        }

        public object Merge(object obj)
        {
            return session.Merge(obj);
        }

        public object Merge(string entityName, object obj)
        {
            return session.Merge(entityName, obj);
        }

        public T Merge<T>(T entity) where T : class
        {
            return session.Merge(entity);
        }

        public T Merge<T>(string entityName, T entity) where T : class
        {
            return session.Merge(entityName, entity);
        }

        public void Persist(object obj)
        {
            session.Persist(obj);
        }

        public void Persist(string entityName, object obj)
        {
            session.Persist(entityName, obj);
        }

        public void Delete(object obj)
        {
            session.Delete(obj);
        }

        public void Delete(string entityName, object obj)
        {
            session.Delete(entityName, obj);
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

        /// <inheritdoc />
        public void JoinTransaction()
        {
            session.JoinTransaction();
        }

        public ICriteria CreateCriteria<T>() where T : class
        {
            return session.CreateCriteria<T>();
        }

        public ICriteria CreateCriteria<T>(string alias) where T : class
        {
            return session.CreateCriteria<T>(alias);
        }

        public ICriteria CreateCriteria(System.Type persistentClass)
        {
            return session.CreateCriteria(persistentClass);
        }

        public ICriteria CreateCriteria(System.Type persistentClass, string alias)
        {
            return session.CreateCriteria(persistentClass, alias);
        }

        public ICriteria CreateCriteria(string entityName)
        {
            return session.CreateCriteria(entityName);
        }

        public ICriteria CreateCriteria(string entityName, string alias)
        {
            return session.CreateCriteria(entityName, alias);
        }

        public IQueryOver<T, T> QueryOver<T>() where T : class
        {
            return session.QueryOver<T>();
        }

        public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
        {
            return session.QueryOver(alias);
        }

        public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
        {
            return session.QueryOver<T>(entityName);
        }

        public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class
        {
            return session.QueryOver(entityName, alias);
        }

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

        /// <inheritdoc />
        public IQueryable<T> Query<T>()
        {
            return session.Query<T>();
        }

        #endregion

        #region IFullTextSession Members

        public IFullTextQuery CreateFullTextQuery<TEntity>(string defaultField, string queryString)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                QueryParser queryParser = new QueryParser(LuceneVersion.LUCENE_48, defaultField, new StandardAnalyzer(LuceneVersion.LUCENE_48));
                Lucene.Net.Search.Query query = queryParser.Parse(queryString);
                return CreateFullTextQuery(query, typeof(TEntity));
            }
        }

        public IFullTextQuery CreateFullTextQuery<TEntity>(string queryString)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                QueryParser queryParser = new QueryParser(LuceneVersion.LUCENE_48, string.Empty, new StandardAnalyzer(LuceneVersion.LUCENE_48));
                Lucene.Net.Search.Query query = queryParser.Parse(queryString);
                return CreateFullTextQuery(query, typeof(TEntity));
            }
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
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                return new FullTextQueryImpl(luceneQuery, entities, session, null);
            }
        }

        /// <summary>
        /// (re)index an entity.
        /// Non indexable entities are ignored
        /// The entity must be associated with the session
        /// </summary>
        /// <param name="entity">The entity to index - must not be null</param>
        /// <returns></returns>
        public IFullTextSession Index(object entity)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                if (entity == null)
                {
                    return this;
                }

                System.Type clazz = NHibernateUtil.GetClass(entity);
                ISearchFactoryImplementor searchFactoryImplementor = SearchFactoryImplementor;

                // TODO: Cache that at the FTSession level
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
        }

        public void PurgeAll(System.Type clazz)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                Purge(clazz, null);
            }
        }

        public void Purge(System.Type clazz, object id)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
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
        }

        public void Dispose()
        {
            session.Dispose();
        }

        #endregion
    }
}