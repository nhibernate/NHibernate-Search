using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

namespace NHibernate.Search.Cfg
{
  public class NHSConfigCollection : Dictionary<string, INHSConfiguration>, INHSConfigCollection
  {
    private const string CfgSchemaResource = "NHibernate.Search.Cfg.nhs-configuration.xsd";
    private readonly XmlSchema configSchema = ReadXmlSchemaFromEmbeddedResource(CfgSchemaResource);

    public NHSConfigCollection()
    {
    }
        /// <summary>
        /// Initializes a new instance of the <see cref="NHSConfiguration"/> class.
        /// </summary>
        /// <param name="configurationReader">The XML reader to parse.</param>
        /// <remarks>
        /// The nhs-configuration.xsd is applied to the XML.
        /// </remarks>
        /// <exception cref="SearchConfigurationException">When nhs-configuration.xsd can't be applied.</exception>
        public NHSConfigCollection(XmlReader configurationReader) {
            XPathNavigator nav;
            try {
                nav = new XPathDocument(XmlReader.Create(configurationReader, GetSettings())).CreateNavigator();
            }
            catch (SearchConfigurationException) {
                throw;
            }
            catch (Exception e) {
                // Encapsule and reThrow
                throw new SearchConfigurationException(e);
            }

            Parse(nav);
        }
    
        public bool HasDefaultConfiguration {
          get {
            //If there is no default configuration then return false
            return DefaultConfiguration != null;
          }
        }

        public INHSConfiguration DefaultConfiguration
        {
          get { 
              //Return if it exists with an empty key.
              if(ContainsKey(string.Empty))
                return this[string.Empty];

              //Default doesn't exist, return null
              return null;
          }
        }

        public INHSConfiguration GetConfiguration(string sessionFactoryName) {
          if (ContainsKey(sessionFactoryName))
            return this[sessionFactoryName];

          return DefaultConfiguration;
        }

        private XmlReaderSettings GetSettings() {
            XmlReaderSettings xmlrs = CreateConfigReaderSettings();
            return xmlrs;
        }

        private XmlReaderSettings CreateConfigReaderSettings() {
            XmlReaderSettings result = CreateXmlReaderSettings(configSchema);
            result.ValidationEventHandler += ConfigSettingsValidationEventHandler;
            result.IgnoreComments = true;
            return result;
        }

        private static XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema) {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas.Add(xmlSchema);
            return settings;
        }

        private static XmlSchema ReadXmlSchemaFromEmbeddedResource(string resourceName) {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            using (Stream resourceStream = executingAssembly.GetManifestResourceStream(resourceName))
                return XmlSchema.Read(resourceStream, null);
        }

        private static void ConfigSettingsValidationEventHandler(object sender, ValidationEventArgs e) {
            throw new SearchConfigurationException("An exception occurred parsing configuration :" + e.Message,
                                                   e.Exception);
        }

        private void Parse(XPathNavigator navigator) {
            XPathNodeIterator xpni = navigator.Select(CfgXmlHelper.SearchFactoryExpression);
            while (xpni.MoveNext()) {
                XPathNavigator pNav = xpni.Current.Clone();
                NHSConfiguration config = new NHSConfiguration(pNav);

                //Check to see if we already have a search factory configuration for this session factory instance.
                if (ContainsKey(config.SessionFactoryName))
                  throw new AmbiguousSearchCfgException(
                    "Ambiguous sessionFactoryName. Please specify only one search factory per NHbernate session factory");

                Add(config.SessionFactoryName, config);
            }
        }
  }
}
