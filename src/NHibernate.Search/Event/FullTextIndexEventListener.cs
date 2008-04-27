using System;
using NHibernate.Cfg;
using NHibernate.Event;
using NHibernate.Search.Backend;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;

namespace NHibernate.Search.Event {
    public class FullTextIndexEventListener : IPostDeleteEventListener, IPostInsertEventListener,
                                              IPostUpdateEventListener,
                                              IInitializable {
        protected SearchFactory searchFactory;
        protected bool used;

        public SearchFactory SearchFactory {
            get { return searchFactory; }
        }

        #region IInitializable Members

        public void Initialize(Configuration cfg) {
            searchFactory = SearchFactory.GetSearchFactory(cfg);

            String indexingStrategy = cfg.GetProperty(Environment.IndexingStrategy);
            if (indexingStrategy == null)
                indexingStrategy = "event";
            if ("event".Equals(indexingStrategy)) used = searchFactory.DocumentBuilders.Count != 0;
            else if ("manual".Equals(indexingStrategy)) used = false;
            else throw new SearchException(Environment.IndexBase + " unknown: " + indexingStrategy);
        }

        #endregion

        #region IPostDeleteEventListener Members

        public void OnPostDelete(PostDeleteEvent e) {
            if (used && EntityIsIndexed(e.Entity))
                processWork(e.Entity, e.Id, WorkType.Delete, e);
        }

        #endregion

        #region IPostInsertEventListener Members

        public void OnPostInsert(PostInsertEvent e) {
            if (used) {
                Object entity = e.Entity;
                //not strictly necessary but a smal optimization
                if (EntityIsIndexed(entity)) processWork(entity, e.Id, WorkType.Add, e);
            }
        }

        #endregion

        #region IPostUpdateEventListener Members

        public void OnPostUpdate(PostUpdateEvent e) {
            if (used) {
                Object entity = e.Entity;
                //not strictly necessary but a smal optimization

                if (EntityIsIndexed(entity))
                    processWork(entity, e.Id, WorkType.Update, e);
            }
        }

        #endregion

        private bool EntityIsIndexed(object entity) {
            return searchFactory.GetDocumentBuilder(entity) != null;
        }

        protected void processWork(Object entity, object id, WorkType workType, AbstractEvent e) {
            searchFactory.PerformWork(entity, id, e.Session, workType);
        }
                                              }
}