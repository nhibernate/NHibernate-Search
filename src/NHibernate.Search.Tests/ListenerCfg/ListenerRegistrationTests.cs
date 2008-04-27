using System.Collections;
using NHibernate.Search.Event;
using NUnit.Framework;

namespace NHibernate.Search.Tests.ListenerCfg {
    [TestFixture]
    public class ListenerRegistrationTests : SearchTestCase {
        protected override IList Mappings {
            get { return new string[0]; }
        }

        [Test]
        public void PostInsertEventListenerTest() {
            cfg.Configure();
            Assert.AreEqual(1, cfg.EventListeners.PostInsertEventListeners.Length);
            Assert.IsTrue(cfg.EventListeners.PostInsertEventListeners[0] is FullTextIndexEventListener);
            Assert.Greater(cfg.EventListeners.PostInsertEventListeners.Length, 0);
        }
    }
}