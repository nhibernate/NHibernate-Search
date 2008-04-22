using Lucene.Net.Analysis;
using Lucene.Net.Store;
using NHibernate.Cfg;
using NHibernate.Event;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Search.Storage;
using NUnit.Framework;

namespace NHibernate.Search.Tests
{
	[TestFixture]
	public abstract class SearchTestCase : NHibernate.Test.TestCase
	{
		protected Directory GetDirectory(System.Type clazz)
		{
			return SearchFactory.GetSearchFactory(cfg).GetDirectoryProvider(clazz).Directory;
		}

		protected override void Configure(Configuration configuration)
		{
			cfg.SetProperty("hibernate.search.default.directory_provider", typeof (RAMDirectoryProvider).AssemblyQualifiedName);
			cfg.SetProperty(Environment.AnalyzerClass, typeof (StopAnalyzer).AssemblyQualifiedName);
			SetListener(cfg);
		}

		public static void SetListener(Configuration configure) {
			configure.SetListener(ListenerType.PostUpdate, new NHibernate.Search.Event.FullTextIndexEventListener());
			configure.SetListener(ListenerType.PostInsert, new NHibernate.Search.Event.FullTextIndexEventListener());
			configure.SetListener(ListenerType.PostDelete, new NHibernate.Search.Event.FullTextIndexEventListener());
		}

		protected override string MappingsAssembly
		{
			get { return "NHibernate.Search.Tests"; }
		}
 
	}
}