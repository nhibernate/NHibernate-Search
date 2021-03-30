using Lucene.Net.Search;
using NHibernate.Engine.Query;
using NHibernate.Impl;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Search.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using J2N.Collections.ObjectModel;

namespace NHibernate.Search.Query
{
    using Filter;

    using Transform;

    public class FullTextQueryImpl : QueryImpl, IFullTextQuery
    {
        private static readonly ILoader noLoader = new NoLoader();
        private readonly INHibernateLogger log;
        private readonly Dictionary<string, FullTextFilterImpl> filterDefinitions;
        private readonly Lucene.Net.Search.Query luceneQuery;
        private System.Type[] classes;
        private ISet<System.Type> classesAndSubclasses;
        private int resultSize;
        private Sort sort;
        private Lucene.Net.Search.Filter filter;
        private ICriteria criteria;
        private string[] indexProjection;
        private IResultTransformer resultTransformer;
        private ISearchFactoryImplementor searchFactoryImplementor;
        private int _maxResults = 20;  //Environment.MaxResults;

        #region Constructors

        /// <summary>
        /// classes must be immutable
        /// </summary>
        public FullTextQueryImpl(Lucene.Net.Search.Query query,
                                 System.Type[] classes,
                                 ISession session,
                                 ParameterMetadata parameterMetadata,
                                 INHibernateLogger log = null)
            : base(query?.ToString() ?? throw new ArgumentNullException(nameof(query)),
                FlushMode.Unspecified,
                session?.GetSessionImplementation() ?? throw new ArgumentNullException(nameof(session)),
                parameterMetadata) // ?? throw new ArgumentNullException(nameof(parameterMetadata)))
        {
            this.log = log ?? NHibernateLogger.For(typeof(FullTextQueryImpl));
            luceneQuery = query;
            resultSize = -1;
            this.classes = classes;
            this.filterDefinitions = new Dictionary<string, FullTextFilterImpl>();
        }

        #endregion


        public int ResultSize => GetResultSize();

        protected override IDictionary<string, LockMode> LockModes
        {
            get { return null; }
        }

        private ISearchFactoryImplementor SearchFactory
        {
            get
            {
                if (searchFactoryImplementor == null)
                    searchFactoryImplementor = ContextHelper.GetSearchFactoryBySFI(Session);
                return searchFactoryImplementor;
            }
        }

        public IFullTextQuery SetSort(Sort value)
        {
            this.sort = value;
            return this;
        }

        public IFullTextQuery SetFilter(Lucene.Net.Search.Filter value)
        {
            this.filter = value;
            return this;
        }

        public override IQuery SetLockMode(string alias, LockMode lockMode)
        {
            return null;
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<T>> EnumerableAsync<T>(CancellationToken cancellationToken = default) => 
            (IEnumerable<T>)await ListAsyncImpl<T>(null, cancellationToken);

        /// <inheritdoc />
        public override async Task<IEnumerable> EnumerableAsync(CancellationToken cancellationToken = default) => await EnumerableAsync<Object>(cancellationToken);

        public override IEnumerable<T> Enumerable<T>()
        {
            using (SessionIdLoggingContext.CreateOrNull(Session.SessionId))
            {
                //implement an interator which keep the id/class for each hit and get the object on demand
                //cause I can't keep the searcher and hence the hit opened. I dont have any hook to know when the
                //user stop using it
                //scrollable is better in this area

                //find the directories
                IndexSearcher searcher = BuildSearcher();
                if (searcher == null)
                {
                    return new IteratorImpl<T>(new List<EntityInfo>(), noLoader).Iterate();
                }

                try
                {
                    var infos = ExtractEntityInfos(searcher);
                    return new IteratorImpl<T>(infos, GetLoader((ISession)Session)).Iterate();
                }
                catch (IOException e)
                {
                    throw new HibernateException("Unable to query Lucene index", e);
                }
                finally
                {
                    CloseSearcher(searcher);
                }
            }
        }

        public override IEnumerable Enumerable()
        {
            using (SessionIdLoggingContext.CreateOrNull(Session.SessionId))
            {
                return Enumerable<object>();
            }
        }

        /// <inheritdoc />
        public override async Task<IList<T>> ListAsync<T>(CancellationToken cancellationToken = default)
        {
            using (SessionIdLoggingContext.CreateOrNull(Session.SessionId))
            {
                return (IList<T>)await ListAsyncImpl<T>(null, cancellationToken);
            }
        }

        private async Task<IList> ListAsyncImpl<T>(IList list = default, CancellationToken cancellationToken = default)
        {
            //implement an interator which keep the id/class for each hit and get the object on demand
            //cause I can't keep the searcher and hence the hit opened. I dont have any hook to know when the
            //user stop using it
            //scrollable is better in this area

            //find the directories
            IndexSearcher searcher = BuildSearcher();
            if (searcher == null)
            {
                return new T[0];
            }

            try
            {
                var infos = ExtractEntityInfos(searcher);
                IList entities = list ?? new List<T>(infos.Count);
                await foreach (var entity in new AsyncIteratorImpl<T>((IReadOnlyList<EntityInfo>) infos,
                    GetAsyncLoader((ISession) Session)).IterateAsync(cancellationToken))
                {
                    entities.Add(entity);
                }

                return entities;
            }
            catch (IOException e)
            {
                throw new HibernateException("Unable to query Lucene index", e);
            }
            finally
            {
                CloseSearcher(searcher);
            }
        }

        /// <inheritdoc />
        public override async Task<IList> ListAsync(CancellationToken cancellationToken = default) =>
            await ListAsyncImpl<object>(null, cancellationToken);

        /// <inheritdoc />
        public override async Task ListAsync(IList results, CancellationToken cancellationToken = default) => 
            await ListAsyncImpl<object>(results, cancellationToken);

        public override IList<T> List<T>()
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                ArrayList arrayList = new ArrayList();
                List(arrayList);
                return (T[])arrayList.ToArray(typeof(T));
            }
        }

        public override IList List()
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                ArrayList arrayList = new ArrayList();
                List(arrayList);
                return arrayList;
            }
        }

        public override void List(IList list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            using (new SessionIdLoggingContext(Session.SessionId))
            {
                // Find the directories
                IndexSearcher searcher = BuildSearcher();

                if (searcher == null)
                {
                    return;
                }

                try
                {
                    IList<EntityInfo> infos = ExtractEntityInfos(searcher);
                    ISession sess = (ISession)Session;
                    ILoader loader = GetLoader(sess);
                    IList entities = loader.Load(infos.ToArray());
                    foreach (object entity in entities)
                    {
                        list.Add(entity);
                    }

                    if (entities.Count != infos.Count)
                        log.Warn("Lucene index contains infos about {0} entities, but were found in the database. Rebuild the index.", infos.Count, entities.Count);

                    if (resultTransformer == null || loader is ProjectionLoader)
                    {
                        // stay consistent with transformTuple which can only be executed during a projection
                    }
                    else
                    {
                        IList tempList = resultTransformer.TransformList(list);
                        list.Clear();
                        foreach (object entity in tempList)
                        {
                            list.Add(entity);
                        }
                    }

                }
                catch (IOException e)
                {
                    throw new HibernateException("Unable to query Lucene index", e);
                }
                finally
                {
                    CloseSearcher(searcher);
                }
            }
        }

        public override int ExecuteUpdate()
        {
            throw new HibernateException("Not supported operation");
        }

        public IFullTextQuery SetCriteriaQuery(ICriteria value)
        {
            this.criteria = value;
            return this;
        }

        public IFullTextQuery SetProjection(params string[] fields)
        {
            this.indexProjection = fields == null || fields.Length == 0 ? null : fields;

            return this;
        }

        public IFullTextFilter EnableFullTextFilter(string name)
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                if (filterDefinitions.TryGetValue(name, out var filterDefinition))
                {
                    return filterDefinition;
                }

                filterDefinition = new FullTextFilterImpl { Name = name };
                FilterDef filterDef = SearchFactory.GetFilterDefinition(name);
                if (filterDef == null)
                {
                    throw new SearchException("Unknown FullTextFilter: " + name);
                }

                filterDefinitions[name] = filterDefinition;

                return filterDefinition;
            }
        }

        public void DisableFullTextFilter(string name)
        {
            filterDefinitions.Remove(name);
        }

        public new IFullTextQuery SetFirstResult(int value)
        {
            // NB Base owns this value via Selection instance
            base.SetFirstResult(value);
            return this;
        }

        public new IFullTextQuery SetMaxResults(int value)
        {
            // NB Base owns this value via Selection instance
            base.SetMaxResults(value);
            return this;
        }

        public new IFullTextQuery SetFetchSize(int value)
        {
            // NB Base owns this value via Selection instance
            base.SetFetchSize(value);
            return this;
        }

        public new IFullTextQuery SetResultTransformer(IResultTransformer transformer)
        {
            base.SetResultTransformer(transformer);
            this.resultTransformer = transformer;
            return this;
        }

        private IList<EntityInfo> ExtractEntityInfos(IndexSearcher searcher)
        {
            TopDocs hits = GetTopDocs(searcher);
            SetResultSize(hits);
            int first = First();
            int max = Max(first, hits);
            int size = Math.Max(0, max - first + 1);
            IList<EntityInfo> infos = new List<EntityInfo>(size);

            if (size <= 0)
            {
                return infos;
            }

            DocumentExtractor extractor = new DocumentExtractor(SearchFactory, indexProjection);
            for (int index = first; index <= max; index++)
            {
                //TODO use indexSearcher.getIndexReader().document( hits.id(index), FieldSelector(indexProjection) );
                infos.Add(extractor.Extract(hits, searcher, index));
            }

            return infos;
        }

        // Previously: Hits GetHits(IndexSearcher searcher)
        private TopDocs GetTopDocs(IndexSearcher searcher)
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                LogQuery();
                var query = FullTextSearchHelper.FilterQueryByClasses(classesAndSubclasses, luceneQuery);
                BuildFilters();
                TopDocs topDocs = searcher.Search(query, filter, _maxResults, this.sort ?? Sort.RELEVANCE);
                log.Debug("Lucene query returned {0} results", topDocs.TotalHits);
                this.SetResultSize(topDocs);

                return topDocs;
            }
        }

        private IAsyncLoader GetAsyncLoader(ISession session)
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                return (IAsyncLoader)GetLoader(session);
            }
        }

        private ILoader GetLoader(ISession session)
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                if (indexProjection != null)
                {
                    ProjectionLoader loader = new ProjectionLoader();
                    loader.Init(session, SearchFactory, resultTransformer, indexProjection);
                    return loader;
                }

                if (criteria != null)
                {
                    if (classes.GetLength(0) > 1)
                    {
                        throw new SearchException("Cannot mix criteria and multiple entity types");
                    }

                    if (criteria is CriteriaImpl)
                    {
                        string targetEntity = ((CriteriaImpl)criteria).EntityOrClassName;
                        if (classes.GetLength(0) == 1 && classes[0].FullName != targetEntity)
                        {
                            throw new SearchException("Criteria query entity should match query entity");
                        }

                        try
                        {
                            System.Type entityType = ((CriteriaImpl)criteria).GetRootEntityTypeIfAvailable();
                            classes = new System.Type[] { entityType };
                        }
                        catch (Exception e)
                        {
                            throw new SearchException("Unable to load entity class from criteria: " + targetEntity, e);
                        }
                    }

                    QueryLoader loader = new QueryLoader();
                    loader.Init(session, searchFactoryImplementor);
                    loader.EntityType = classes[0];
                    loader.Criteria = criteria;
                    return loader;
                }

                if (classes.GetLength(0) == 1)
                {
                    QueryLoader loader = new QueryLoader();
                    loader.Init(session, searchFactoryImplementor);
                    loader.EntityType = classes[0];
                    return loader;
                }
                else
                {
                    ObjectLoader loader = new ObjectLoader();
                    loader.Init(session, searchFactoryImplementor);
                    return loader;
                }
            }
        }

        private int GetResultSize()
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                if (resultSize < 0)
                {
                    //get result size without object initialization
                    IndexSearcher searcher = BuildSearcher();

                    if (searcher == null)
                        resultSize = 0;
                    else
                        try
                        {
                            resultSize = GetTopDocs(searcher).TotalHits;
                        }
                        catch (IOException e)
                        {
                            throw new HibernateException("Unable to query Lucene index", e);
                        }
                        finally
                        {
                            CloseSearcher(searcher);
                        }
                }
                return resultSize;
            }
        }

        private void LogQuery()
        {
            if (!log.IsDebugEnabled())
            {
                return;
            }

            var sb = new StringBuilder();
            if (classesAndSubclasses == null)
            {
                sb.Append("All");
            }
            else
            {
                foreach (var type in classesAndSubclasses)
                {
                    sb.Append(type.Name).Append("|");
                }
            }

            int maxRows;
            int firstRow;
            if (RowSelection != null)
            {
                maxRows = RowSelection.MaxRows;
                firstRow = RowSelection.FirstRow;
            }
            else
            {
                maxRows = -1;
                firstRow = -1;
            }

            log.Debug("Execute lucene query [{0}]: {1}. Max rows: {2}, First result: {3}", sb, luceneQuery, maxRows, firstRow);
        }

        private void BuildFilters()
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                if (filterDefinitions.Count > 0)
                {
                    ChainedFilter chainedFilter = new ChainedFilter();
                    foreach (FullTextFilterImpl filterDefinition in filterDefinitions.Values)
                    {
                        FilterDef def = SearchFactory.GetFilterDefinition(filterDefinition.Name);
                        object instance;
                        try
                        {
                            instance = Activator.CreateInstance(def.Impl);
                        }
                        catch (Exception e)
                        {
                            throw new HibernateException("Unable to instantiate FullTextFilterDef: " + def.Impl.FullName, e);
                        }

                        foreach (KeyValuePair<string, object> entry in filterDefinition.Parameters)
                        {
                            def.Invoke(entry.Key, instance, entry.Value);
                        }

                        if (def.Cache && def.KeyMethod == null && filterDefinition.Parameters.Count > 0)
                            throw new SearchException("Filter with parameters and no Key method: " + filterDefinition.Name);

                        FilterKey key = null;
                        if (def.Cache)
                        {
                            if (def.KeyMethod == null)
                            {
                                key = new ImplFilterKey();
                            }
                            else
                            {
                                try
                                {
                                    key = (FilterKey)def.KeyMethod.Invoke(instance, null);
                                }
                                catch (InvalidCastException)
                                {
                                    throw new SearchException("Key method does not return FilterKey: " + def.Impl.Name + "." + def.KeyMethod.Name);
                                }
                                // TODO: More specific exception filtering here
                                catch (Exception e)
                                {
                                    throw new SearchException("Error accessing Key method: " + def.Impl.Name + "." + def.KeyMethod.Name, e);
                                }
                            }

                            key.Impl = def.Impl;
                        }

                        Lucene.Net.Search.Filter f = def.Cache
                                                        ? SearchFactory.GetFilterCachingStrategy().GetCachedFilter(key)
                                                        : null;

                        if (f == null)
                        {
                            if (def.FactoryMethod != null)
                            {
                                try
                                {
                                    f = (Lucene.Net.Search.Filter)def.FactoryMethod.Invoke(instance, null);
                                }
                                catch (InvalidCastException)
                                {
                                    throw new SearchException("Factory method does not return Lucene.Net.Search.Filter: " + def.FactoryMethod.Name);
                                }
                                // TODO: More specific exception filtering here
                                catch (Exception e)
                                {
                                    throw new SearchException("Error accessing Factory method: " + def.FactoryMethod.Name, e);
                                }
                            }
                            else
                            {
                                try
                                {
                                    f = (Lucene.Net.Search.Filter)instance;
                                }
                                catch (InvalidCastException)
                                {
                                    throw new SearchException("Class is not a Lucene.Net.Search.Filter: " + def.Impl.Name);
                                }
                            }

                            if (def.Cache)
                            {
                                SearchFactory.GetFilterCachingStrategy().AddCachedFilter(key, f);
                            }
                        }

                        chainedFilter.AddFilter(f);
                    }

                    if (filter != null)
                    {
                        chainedFilter.AddFilter(filter);
                    }

                    filter = chainedFilter;
                }
            }
        }

        private void CloseSearcher(IndexSearcher searcher)
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                if (searcher == null)
                {
                    return;
                }

                try
                {
                    SearchFactory.ReaderProvider.CloseReader(searcher.IndexReader);
                }
                catch (IOException e)
                {
                    log.Warn(e, "Unable to properly close searcher during lucene query: {0}", QueryString);
                }
            }
        }

        private IndexSearcher BuildSearcher()
        {
            // Java version is inline, but we need the same code in LuceneQueryExpression
            return FullTextSearchHelper.BuildSearcher(SearchFactory, out classesAndSubclasses, classes);
        }

        private int Max(int first, TopDocs topDocs)
        {
            if (Selection.MaxRows == NHibernate.Engine.RowSelection.NoValue)
            {
                return topDocs.TotalHits - 1;
            }

            if (Selection.MaxRows + first < topDocs.TotalHits)
            {
                return first + Selection.MaxRows - 1;
            }

            return topDocs.TotalHits - 1;
        }

        private int First()
        {
            return Selection.FirstRow != NHibernate.Engine.RowSelection.NoValue ? Selection.FirstRow : 0;
        }

        private void SetResultSize(TopDocs topDocs)
        {
            resultSize = topDocs.TotalHits;
        }

        #region Nested type: ImplFilterKey

        private class ImplFilterKey : FilterKey
        {
            public override int GetHashCode()
            {
                return Impl.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }

                FilterKey key = obj as FilterKey;
                return key != null && this.Impl.Equals(key.Impl);
            }
        }

        #endregion

        #region Nested type: NoLoader

        private class NoLoader : ILoader
        {
            public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
            {
            }

            public object Load(EntityInfo entityInfo)
            {
                throw new NotSupportedException("noLoader should not be used");
            }

            public IList Load(params EntityInfo[] entityInfos)
            {
                throw new NotSupportedException("noLoader should not be used");
            }
        };

        #endregion

        #region Nested type: IteratorImpl

        private class IteratorImpl<T>
        {
            private readonly IList<EntityInfo> entityInfos;
            private readonly ILoader loader;

            public IteratorImpl(IList<EntityInfo> entityInfos, ILoader loader)
            {
                this.entityInfos = entityInfos;
                this.loader = loader;
            }

            public IEnumerable<T> Iterate()
            {
                foreach (EntityInfo entityInfo in entityInfos)
                    yield return (T)loader.Load(entityInfo);
            }
        }

        #endregion

        private class AsyncIteratorImpl<T>
        {
            private readonly IReadOnlyList<EntityInfo> entityInfos;
            private readonly IAsyncLoader loader;

            public AsyncIteratorImpl(IReadOnlyList<EntityInfo> entityInfos, IAsyncLoader loader)
            {
                this.entityInfos = entityInfos;
                this.loader = loader;
            }

            public async IAsyncEnumerable<T> IterateAsync([EnumeratorCancellation] CancellationToken cancellationToken)
            {
                foreach (var entityInfo in entityInfos)
                    yield return (T)await loader.LoadAsync(entityInfo, cancellationToken);
            }
        }
    }
}