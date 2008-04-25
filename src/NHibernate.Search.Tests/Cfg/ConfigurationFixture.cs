using System.Xml;
using NHibernate.Search.Cfg;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Cfg {
	[TestFixture]
	public class ConfigurationFixture {
		[Test, ExpectedException(typeof (SearchConfigurationException))]
		public void BadSchema() {
			string xml =
				@"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
		<unkonwednode/>
	</nhs-configuration>";
			XmlDocument cfgXml = new XmlDocument();
			cfgXml.LoadXml(xml);
			XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
			new NHSConfiguration(xtr);
		}

		[Test]
		public void IgnoreEmptyConfiguration() {
			string xml =
				@"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
	</nhs-configuration>";
			XmlDocument cfgXml = new XmlDocument();
			cfgXml.LoadXml(xml);
			XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
			NHSConfiguration cfg = new NHSConfiguration(xtr);
			Assert.AreEqual(0, cfg.Properties.Count);
		}

		[Test]
		public void IgnoreEmptyItems() {
			string xml =
				@"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
		<property name='hibernate.search.default.directory_provider'></property>
		<property name='hibernate.search.default.indexBase'></property>
	</nhs-configuration>";
			XmlDocument cfgXml = new XmlDocument();
			cfgXml.LoadXml(xml);
			XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
			NHSConfiguration cfg = new NHSConfiguration(xtr);
			Assert.AreEqual(0, cfg.Properties.Count);
		}

		[Test]
		public void WellFormedConfiguration() {
			string xml =
				@"<nhs-configuration xmlns='urn:nhs-configuration-1.0'>
		<property name='hibernate.search.default.directory_provider'>NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search</property>
		<property name='hibernate.search.default.indexBase'>/Index</property>
	</nhs-configuration>";
			XmlDocument cfgXml = new XmlDocument();
			cfgXml.LoadXml(xml);
			XmlTextReader xtr = new XmlTextReader(xml, XmlNodeType.Document, null);
			NHSConfiguration cfg = new NHSConfiguration(xtr);
			Assert.AreEqual(2, cfg.Properties.Count);
			Assert.AreEqual("/Index", cfg.Properties["hibernate.search.default.indexBase"]);
			Assert.AreEqual("NHibernate.Search.Storage.FSDirectoryProvider, NHibernate.Search",
			                cfg.Properties["hibernate.search.default.directory_provider"]);
		}
	}
}