using System.Collections;
using System.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Search.Store;
using NHibernate.Test;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Bridge
{
    [TestFixture]
    public class UnresolvedBridgeTest : SearchTestCase
    {
        protected override IEnumerable<string> Mappings
        {
            get { return new string[] {}; }
        }

        [Test]
        public void SystemTypeForDocumentId()
        {
            Configuration tempCfg = new Configuration();
            tempCfg.Configure(TestConfigurationHelper.GetDefaultConfigurationFilePath());
            tempCfg.SetProperty("hibernate.search.default.directory_provider", typeof(RAMDirectoryProvider).AssemblyQualifiedName);
            tempCfg.AddClass(typeof(Gangster));
			Assert.Throws<HibernateException>(()=>tempCfg.BuildSessionFactory(),"Unable to guess IFieldBridge for Id");
        }
    }
}