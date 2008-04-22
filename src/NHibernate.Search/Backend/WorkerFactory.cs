using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NHibernate.Cfg;
using NHibernate.Search.Engine;

namespace NHibernate.Search.Backend
{
	public static class WorkerFactory
	{
		public static IWorker CreateWorker(Configuration cfg, SearchFactory searchFactory) {
			IWorker worker = new Backend.Impl.TransactionalWorker();
			worker.Initialize((IDictionary) cfg.Properties, searchFactory);
			return worker;
		}
	}
}
