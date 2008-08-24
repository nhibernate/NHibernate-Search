using System.Collections;
using NHibernate.Cfg;
using NHibernate.Search.Impl;
using NHibernate.Search.Store;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Bridge
{
    [TestFixture]
    public class UnresolvedBridgeTest : SearchTestCase
    {
        protected override IList Mappings
        {
            get { return new string[] {"Bridge.Gangster.hbm.xml"}; }
        }

        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            configuration.SetProperty("hibernate.search.default.directory_provider", typeof(RAMDirectoryProvider).AssemblyQualifiedName);
        }

        [Test, ExpectedException(typeof(SearchException)), Ignore("Which is the undefined bridge on Gangster?")]
        public void SerializableType()
        {
            
        }
    }
}