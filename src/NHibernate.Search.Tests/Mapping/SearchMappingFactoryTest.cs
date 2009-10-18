using System;
using System.Collections.Generic;

using NHibernate.Cfg;
using NHibernate.Search.Mapping;
using NHibernate.Search.Mapping.AttributeBased;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Mapping
{
    [TestFixture]
    public class SearchMappingFactoryTest
    {
        private class TestSearchMapping : ISearchMapping
        {
            public ICollection<DocumentMapping> Build(Configuration cfg)
            {
                throw new NotSupportedException();
            }
        }

        [Test]
        public void TestCreateCreatesAttributeMappingByDefault()
        {
            Assert.IsInstanceOf<AttributeSearchMapping>(SearchMappingFactory.CreateMapping(new Configuration()));
        }

        [Test]
        public void TestCreateCreatesConfiguredMapping()
        {
            var cfg = new Configuration();
            cfg.SetProperty("hibernate.search.mapping", typeof(TestSearchMapping).AssemblyQualifiedName);

            Assert.IsInstanceOf<TestSearchMapping>(SearchMappingFactory.CreateMapping(cfg));
        }
    }
}
