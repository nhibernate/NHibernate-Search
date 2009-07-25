using System.Collections;

using NHibernate.Search.Event;

using NUnit.Framework;

namespace NHibernate.Search.Tests.ListenerCfg
{
    [TestFixture]
    public class ListenerRegistrationTests : SearchTestCase
    {
        protected override IList Mappings
        {
            get
            {
                return new string[0];
            }
        }

        [Test]
        public void PostInsertEventListenerTest()
        {
            Assert.AreEqual(1, this.cfg.EventListeners.PostInsertEventListeners.Length);
            Assert.IsTrue(this.cfg.EventListeners.PostInsertEventListeners[0] is FullTextIndexEventListener);
            Assert.Greater(this.cfg.EventListeners.PostInsertEventListeners.Length, 0);
        }
    }
}