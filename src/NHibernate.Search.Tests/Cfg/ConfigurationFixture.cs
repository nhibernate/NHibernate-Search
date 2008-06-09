using System.Xml;
using NHibernate.Search.Cfg;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Cfg {
  [TestFixture]
  public class ConfigurationFixture
  {
    [Test, ExpectedException(typeof(SearchConfigurationException))]
    public void BadSchema()
    {
      string xml =
          @"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
		<unkonwednode/>
	</nhs-configuration>";
      XmlDocument cfgXml = new XmlDocument();
      cfgXml.LoadXml(xml);
      XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
      new NHSConfigCollection(xtr);
    }

    [Test]
    public void IgnoreEmptyConfiguration()
    {
      string xml =
          @"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
	</nhs-configuration>";
      XmlDocument cfgXml = new XmlDocument();
      cfgXml.LoadXml(xml);
      XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
      NHSConfigCollection cfgCollection = new NHSConfigCollection(xtr);
      Assert.AreEqual(0, cfgCollection.Count);
    }

    [Test]
    public void IgnoreEmptyItems()
    {
      string xml =
          @"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
              <search-factory>
		            <property name='hibernate.search.default.directory_provider'></property>
		            <property name='hibernate.search.default.indexBase'></property>
              </search-factory>
      	  </nhs-configuration>";
      XmlDocument cfgXml = new XmlDocument();
      cfgXml.LoadXml(xml);
      XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
      NHSConfigCollection cfgCollection = new NHSConfigCollection(xtr);
      INHSConfiguration cfg = cfgCollection.GetConfiguration("");
      Assert.AreEqual(0, cfg.Properties.Count);
    }

    [Test]
    public void WellFormedConfiguration()
    {
      string xml =
          @"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
                      <search-factory>
                        <property name='hibernate.search.default.directory_provider'>NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search</property>
		                    <property name='hibernate.search.default.indexBase'>/Index</property>
                      </search-factory>
              	  </nhs-configuration>";

      XmlDocument cfgXml = new XmlDocument();
      cfgXml.LoadXml(xml);
      XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
      NHSConfigCollection cfgCollection = new NHSConfigCollection(xtr);
      Assert.AreEqual(1, cfgCollection.Count);

      INHSConfiguration cfg = cfgCollection.GetConfiguration("");

      Assert.AreEqual("", cfg.SessionFactoryName);
      Assert.AreEqual("/Index", cfg.Properties["hibernate.search.default.indexBase"]);
      Assert.AreEqual("NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search",
                      cfg.Properties["hibernate.search.default.directory_provider"]);
    }

    [Test]
    public void WellFormedConfigurationWithNamedSessionFactory()
    {
      string xml =
          @"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
                      <search-factory sessionFactoryName='NHibernate.Test'>
                        <property name='hibernate.search.default.directory_provider'>NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search</property>
		                    <property name='hibernate.search.default.indexBase'>/Index</property>
                      </search-factory>
              	  </nhs-configuration>";

      XmlDocument cfgXml = new XmlDocument();
      cfgXml.LoadXml(xml);
      XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
      NHSConfigCollection cfgCollection = new NHSConfigCollection(xtr);
      
      Assert.AreEqual(1, cfgCollection.Count);
      Assert.IsTrue(cfgCollection.ContainsKey("NHibernate.Test"));
      
      INHSConfiguration cfg = cfgCollection.GetConfiguration("NHibernate.Test");
      Assert.AreEqual("NHibernate.Test", cfg.SessionFactoryName);
    }

    [Test]
    public void WellFormedConfigurationWithTwoNamedSessionFactory()
    {
      string xml =
          @"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
                      <search-factory sessionFactoryName='NHibernate.Test'>
                        <property name='hibernate.search.default.directory_provider'>NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search</property>
		                    <property name='hibernate.search.default.indexBase'>/Index</property>
                      </search-factory>
                      <search-factory sessionFactoryName='AnotherSessionFactory'>
                        <property name='hibernate.search.default.directory_provider'>NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search</property>
		                    <property name='hibernate.search.default.indexBase'>/dev/null</property>
                      </search-factory>
              	  </nhs-configuration>";

      XmlDocument cfgXml = new XmlDocument();
      cfgXml.LoadXml(xml);
      XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
      NHSConfigCollection cfgCollection = new NHSConfigCollection(xtr);
      Assert.AreEqual(2, cfgCollection.Count);

      INHSConfiguration firstCfg = cfgCollection.GetConfiguration("NHibernate.Test");
      INHSConfiguration secondCfg = cfgCollection.GetConfiguration("AnotherSessionFactory");

      Assert.AreNotEqual(firstCfg.Properties["hibernate.search.default.indexBase"],
                         secondCfg.Properties["hibernate.search.default.indexBase"]);
      
    }
    [Test, ExpectedException(typeof(AmbiguousSearchCfgException))]
    public void BadConfigurationWithDuplicateNamedSessionFactory()
    {
      string xml =
          @"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
                      <search-factory sessionFactoryName='NHibernate.Test'>
                        <property name='hibernate.search.default.directory_provider'>NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search</property>
		                    <property name='hibernate.search.default.indexBase'>/Index</property>
                      </search-factory>
                      <search-factory sessionFactoryName='NHibernate.Test'>
                        <property name='hibernate.search.default.directory_provider'>NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search</property>
		                    <property name='hibernate.search.default.indexBase'>/dev/null</property>
                      </search-factory>
              	  </nhs-configuration>";

      XmlDocument cfgXml = new XmlDocument();
      cfgXml.LoadXml(xml);
      XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
      new NHSConfigCollection(xtr);
    }

    [Test]
    public void NamedSessionGetsDefaultSearchFactory()
    {
      string xml =
          @"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
                      <search-factory>
                        <property name='hibernate.search.default.directory_provider'>NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search</property>
		                    <property name='hibernate.search.default.indexBase'>/Index</property>
                      </search-factory>
              	  </nhs-configuration>";

      XmlDocument cfgXml = new XmlDocument();
      cfgXml.LoadXml(xml);
      XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
      NHSConfigCollection cfgCollection = new NHSConfigCollection(xtr);
      Assert.AreEqual(1, cfgCollection.Count);

      INHSConfiguration cfg = cfgCollection.GetConfiguration("NHibernate.Test");

      Assert.AreEqual("", cfg.SessionFactoryName);
      Assert.AreEqual("/Index", cfg.Properties["hibernate.search.default.indexBase"]);
      Assert.AreEqual("NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search",
                      cfg.Properties["hibernate.search.default.directory_provider"]);
    }

    [Test]
    public void CollectionHasDefaultSearchFactory()
    {
      string xml =
          @"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
                      <search-factory>
                        <property name='hibernate.search.default.directory_provider'>NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search</property>
		                    <property name='hibernate.search.default.indexBase'>/Index</property>
                      </search-factory>
              	  </nhs-configuration>";

      XmlDocument cfgXml = new XmlDocument();
      cfgXml.LoadXml(xml);
      XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
      NHSConfigCollection cfgCollection = new NHSConfigCollection(xtr);
      Assert.AreEqual(1, cfgCollection.Count);

      Assert.IsTrue(cfgCollection.HasDefaultConfiguration);
    }
  }
}
