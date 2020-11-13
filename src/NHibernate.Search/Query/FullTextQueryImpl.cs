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
using System.Text;

namespace NHibernate.Search.Query
{
    using Filter;

    using Transform;

    public class FullTextQueryImpl : QueryImpl, IFullTextQuery
    {
        private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(FullTextQueryImpl));
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
        public FullTextQueryImpl(Lucene.Net.Search.Query query, System.Type[] classes, ISession session,
                                 ParameterMetadata parameterMetadata)
            : base(query.ToString(), FlushMode.Unspecified, session.GetSessionImplementation(), parameterMetadata)
        {
            luceneQuery = query;
            resultSize = -1;
            this.classes = classes;
            this.filterDefinitions = new Dictionary<string, FullTextFilterImpl>();
        }

        #endregion


        public int ResultSize => GetResultSize();

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

        private TopDocs GetTopDocs(IndexSearcher searcher)
        {
            using (new SessionIdLoggingContext(Session.SessionId))
            {
                LogQuery();
                var query = FullTextSearchHelper.FilterQueryByClasses(classesAndSubclasses, luceneQuery);
                BuildFilters();
                TopDocs topDocs = searcher.Search(query, filter, _maxResults, this.sort ?? Sort.RELEVANCE);
                log.DebugFormat("Lucene query returned {0} results", topDocs.TotalHits);
                this.SetResultSize(topDocs);

                return topDocs;
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
                            System.Type entityType = ((CriteriaImpl) criteria).GetRootEntityTypeIfAvailable();
                            classes = new System.Type[] {entityType};
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
        



    

        //protected override IEnumerable<ITranslator> GetTranslators(ISessionImplementor sessionImplementor, QueryParameters queryParameters)
        //{
        //    throw new NotImplementedException();
        //}
        
        public override IQuery SetLockMode(string alias, LockMode lockMode)
        {
            return null;
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
                FullTextFilterImpl filterDefinition;
                if (filterDefinitions.TryGetValue(name, out filterDefinition))
                {
                    return filterDefinition;
                }

                filterDefinition = new FullTextFilterImpl();
                filterDefinition.Name = name;
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

      

        private void LogQuery()
        {
            if (log.IsDebugEnabled == false)
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

            log.DebugFormat("Execute lucene query [{0}]: {1}. Max rows: {2}, First result: {3}", sb, luceneQuery, maxRows, firstRow);
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
                    log.Warn("Unable to properly close searcher during lucene query: " + QueryString, e);
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

        private static readonly ILoader noLoader = new NoLoader();

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
    }
}