using System.Collections;
using System.Xml;
using NHibernate.Cfg;
using NHibernate.Mapping;
using NUnit.Framework;

namespace NHibernate.Search.Tests.ListenerCfg {
	[TestFixture]
	public class ListenerRegistrationTests : SearchTestCase {
		[Test]
		public void PostInsertEventListenerTest() {
			cfg.Configure();
			Assert.AreEqual(1, cfg.EventListeners.PostInsertEventListeners.Length);
			Assert.IsTrue(cfg.EventListeners.PostInsertEventListeners[0] is NHibernate.Search.Event.FullTextIndexEventListener);
			Assert.Greater(cfg.EventListeners.PostInsertEventListeners.Length, 0);
		}

		protected override IList Mappings {
			get { return new string[0];  }
		}
	}
}