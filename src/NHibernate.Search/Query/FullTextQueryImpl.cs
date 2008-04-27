using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Iesi.Collections.Generic;
using log4net;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using NHibernate.Engine;
using NHibernate.Engine.Query;
using NHibernate.Impl;
using NHibernate.Search.Engine;
using NHibernate.Search.Util;

namespace NHibernate.Search.Query {
    public class FullTextQueryImpl : AbstractQueryImpl, IFullTextQuery {
        private static readonly ILog log = LogManager.GetLogger(typeof (FullTextQueryImpl));
        private readonly System.Type[] classes;
        private readonly Lucene.Net.Search.Query luceneQuery;
        private int batchSize = 1;
        private ISet<System.Type> classesAndSubclasses;
        private int resultSize = -1;
        private SearchFactory searchFactory;
        private Sort sort;

        /// <summary>
        /// classes must be immutable
        /// </summary>
        public FullTextQueryImpl(Lucene.Net.Search.Query query, System.Type[] classes, ISession session,
                                 ParameterMetadata parameterMetadata)
            : base(query.ToString(), FlushMode.Unspecified, session.GetSessionImplementation(), parameterMetadata) {
            luceneQuery = query;
            this.classes = classes;
        }

        protected override IDictionary LockModes {
            get { throw new NotImplementedException("Full Text Query doesn't support lock modes"); }
        }

        private SearchFactory SearchFactory {
            get {
                if (searchFactory == null)
                    searchFactory = ContextHelper.GetSearchFactoryBySFI(Session);
                return searchFactory;
            }
        }

        #region IFullTextQuery Members

        public int ResultSize {
            get {
                if (resultSize < 0) {
                    //get result size without object initialization
                    Searcher searcher = BuildSearcher();

                    if (searcher == null)
                        resultSize = 0;
                    else
                        try {
                            resultSize = GetHits(searcher).Length();
                        }
                        catch (IOException e) {
                            throw new HibernateException("Unable to query Lucene index", e);
                        }
                        finally {
                            CloseSearcher(searcher);
                        }
                }
                return resultSize;
            }
        }

        public override IEnumerable Enumerable() {
            return Enumerable<object>();
        }

        /// <summary>
        /// Return an interator on the results.
        /// Retrieve the object one by one (initialize it during the next() operation)
        /// </summary>
        public override IEnumerable<T> Enumerable<T>() {
            //implement an interator which keep the id/class for each hit and get the object on demand
            //cause I can't keep the searcher and hence the hit opened. I dont have any hook to know when the
            //user stop using it
            //scrollable is better in this area

            //find the directories
            Searcher searcher = BuildSearcher();
            if (searcher == null)
                return new IteratorImpl<T>(new List<EntityInfo>(), Session.GetSession()).Iterate();
            try {
                Hits hits = GetHits(searcher);
                SetResultSize(hits);
                int first = First();
                int max = Max(first, hits);
                IList<EntityInfo> entityInfos = new List<EntityInfo>(max - first + 1);
                for (int index = first; index <= max; index++) {
                    Document document = hits.Doc(index);
                    EntityInfo entityInfo = new EntityInfo();
                    entityInfo.clazz = DocumentBuilder.GetDocumentClass(document);
                    entityInfo.id = DocumentBuilder.GetDocumentId(searchFactory, document);
                    entityInfos.Add(entityInfo);
                }
                return new IteratorImpl<T>(entityInfos, Session.GetSession()).Iterate();
            }
            catch (IOException e) {
                throw new HibernateException("Unable to query Lucene index", e);
            }
            finally {
                CloseSearcher(searcher);
            }
        }

        public override IList<T> List<T>() {
            ArrayList arrayList = new ArrayList();
            List(arrayList);
            return (T[]) arrayList.ToArray(typeof (T));
        }

        public override IList List() {
            ArrayList arrayList = new ArrayList();
            List(arrayList);
            return arrayList;
        }

        public override void List(IList list) {
            //find the directories
            Searcher searcher = BuildSearcher();
            if (searcher == null)
                return;
            try {
                Hits hits = GetHits(searcher);
                SetResultSize(hits);
                int first = First();
                int max = Max(first, hits);
                for (int index = first; index <= max; index++) {
                    Document document = hits.Doc(index);
                    System.Type clazz = DocumentBuilder.GetDocumentClass(document);
                    object id = DocumentBuilder.GetDocumentId(SearchFactory, document);
                    list.Add(Session.GetSession().Load(clazz, id));
                    //use load to benefit from the batch-size
                    //we don't face proxy casting issues since the exact class is extracted from the index
                }
                //then initialize the objects
                IList excludedObects = new ArrayList();
                foreach (Object element in list)
                    try {
                        NHibernateUtil.Initialize(element);
                    }
                    catch (ObjectNotFoundException e) {
                        log.Debug("Object found in Search index but not in database: "
                                  + e.PersistentClass + " with id " + e.Identifier);
                        excludedObects.Add(element);
                    }
                foreach (object excludedObect in excludedObects)
                    list.Remove(excludedObect);
            }
            catch (IOException e) {
                throw new HibernateException("Unable to query Lucene index", e);
            }
            finally {
                CloseSearcher(searcher);
            }
        }

        public override IQuery SetLockMode(string alias, LockMode lockMode) {
            throw new NotImplementedException("Full Text Query doesn't support lock modes");
        }

        public IFullTextQuery SetSort(Sort sort) {
            this.sort = sort;
            return this;
        }

        public override int ExecuteUpdate() {
            // TODO: Implement FullTextQueryImpl.ExecuteUpdate()
            throw new NotImplementedException("Implement FullTextQueryImpl.ExecuteUpdate()");
        }

        #endregion

        private Hits GetHits(Searcher searcher) {
            Lucene.Net.Search.Query query = FullTextSearchHelper.FilterQueryByClasses(classesAndSubclasses, luceneQuery);
            return searcher.Search(query, null, sort);
        }

        private void CloseSearcher(Searcher searcher) {
            if (searcher != null)
                try {
                    searcher.Close();
                }
                catch (IOException e) {
                    log.Warn("Unable to properly close searcher during lucene query: " + QueryString, e);
                }
        }

        private Searcher BuildSearcher() {
            return FullTextSearchHelper.BuildSearcher(SearchFactory, out classesAndSubclasses, classes);
        }

        private int Max(int first, Hits hits) {
            if (Selection.MaxRows == RowSelection.NoValue)
                return hits.Length() - 1;
            else if (Selection.MaxRows + first < hits.Length())
                return first + Selection.MaxRows - 1;
            else return hits.Length() - 1;
        }

        private int First() {
            if (Selection.FirstRow != RowSelection.NoValue)
                return Selection.FirstRow;
            else
                return 0;
        }

        //TODO change classesAndSubclasses by side effect, which is a mismatch with the Searcher return, fix that.

        private void SetResultSize(Hits hits) {
            resultSize = hits.Length();
        }

        #region Nested type: EntityInfo

        private class EntityInfo {
            public System.Type clazz;
            public object id;
        }

        #endregion

        #region Nested type: IteratorImpl

        private class IteratorImpl<T> {
            private IList<EntityInfo> entityInfos;
            private ISession session;

            public IteratorImpl(IList<EntityInfo> entityInfos, ISession session) {
                this.entityInfos = entityInfos;
                this.session = session;
            }

            public IEnumerable<T> Iterate() {
                foreach (EntityInfo entityInfo in entityInfos)
                    yield return (T) session.Load(entityInfo.clazz, entityInfo.id);
            }
        }

        #endregion
    }
}