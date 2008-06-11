using System.Collections;
using NHibernate.Cfg;
using NHibernate.Search.Backend.Impl;
using NHibernate.Search.Impl;

namespace NHibernate.Search.Backend
{
    public static class WorkerFactory
    {
        public static IWorker CreateWorker(Configuration cfg, SearchFactoryImpl searchFactory)
        {
            IWorker worker = new TransactionalWorker();
            worker.Initialize((IDictionary) cfg.Properties, searchFactory);
            return worker;
        }
    }
}