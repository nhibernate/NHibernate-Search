using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using NHibernate.Cfg;

namespace NHibernate.Search.Cfg {
    public class NHSConfiguration : INHSConfiguration {
        private const string CfgSchemaResource = "NHibernate.Search.Cfg.nhs-configuration.xsd";
        private readonly XmlSchema config = ReadXmlSchemaFromEmbeddedResource(CfgSchemaResource);
        private readonly IDictionary<string, string> properties = new Dictionary<string, string>();

        public NHSConfiguration() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="NHSConfiguration"/> class.
        /// </summary>
        /// <param name="configurationReader">The XML reader to parse.</param>
        /// <remarks>
        /// The nhs-configuration.xsd is applied to the XML.
        /// </remarks>
        /// <exception cref="SearchConfigurationException">When nhs-configuration.xsd can't be applied.</exception>
        public NHSConfiguration(XmlReader configurationReader) {
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

        #region INHSConfiguration Members

        public IDictionary<string, string> Properties {
            get { return properties; }
        }

        public string GetProperty(string name) {
            if (properties.ContainsKey(name))
                return properties[name];

            return null;
        }

        /// <summary>
        /// Given an <see cref="INHSConfiguration"/> and a <see cref="Configuration"/>, merge the two dictionaries. 
        /// When a key exists in both dictionaries, replace the <see cref="Configuration"/> value with the <see cref="INHSConfiguration"/> value.
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns>Merged IDictionary<string,string> containing all properties.</returns>
        public IDictionary<string, string> GetMergedProperties(Configuration cfg) {
            IDictionary<string, string> mergedProperties = cfg.Properties;

            foreach (KeyValuePair<string, string> kvPair in properties)
                if (mergedProperties.ContainsKey(kvPair.Key))
                    mergedProperties[kvPair.Key] = kvPair.Value;
                else
                    mergedProperties.Add(kvPair);

            return mergedProperties;
        }

        #endregion

        private XmlReaderSettings GetSettings() {
            XmlReaderSettings xmlrs = CreateConfigReaderSettings();
            return xmlrs;
        }

        private XmlReaderSettings CreateConfigReaderSettings() {
            XmlReaderSettings result = CreateXmlReaderSettings(config);
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
            XPathNodeIterator xpni = navigator.Select(CfgXmlHelper.PropertiesExpression);
            while (xpni.MoveNext()) {
                string propName;
                string propValue = xpni.Current.Value;
                XPathNavigator pNav = xpni.Current.Clone();
                pNav.MoveToFirstAttribute();
                propName = pNav.Value;
                if (!string.IsNullOrEmpty(propName) && !string.IsNullOrEmpty(propValue))
                    properties[propName] = propValue;
            }
        }
    }
}