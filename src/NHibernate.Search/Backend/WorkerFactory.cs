using System.Collections;
using NHibernate.Cfg;
using NHibernate.Search.Backend.Impl;
using NHibernate.Search.Engine;

namespace NHibernate.Search.Backend {
    public static class WorkerFactory {
        public static IWorker CreateWorker(Configuration cfg, SearchFactory searchFactory) {
            IWorker worker = new TransactionalWorker();
            worker.Initialize((IDictionary) cfg.Properties, searchFactory);
            return worker;
        }
    }
}