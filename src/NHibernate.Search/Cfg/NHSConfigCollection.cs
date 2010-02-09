namespace NHibernate.Search.Cfg
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.XPath;

    /// <summary>
    /// The NHibernate Search config collection implementation.
    /// </summary>
    public class NHSConfigCollection : Dictionary<string, INHSConfiguration>, INHSConfigCollection
    {
        private const string CfgSchemaResource = "NHibernate.Search.Cfg.nhs-configuration.xsd";
        private readonly XmlSchema configSchema = ReadXmlSchemaFromEmbeddedResource(CfgSchemaResource);

        /// <summary>
        /// Initializes a new instance of the <see cref="NHSConfigCollection"/> class.
        /// </summary>
        public NHSConfigCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NHSConfigCollection"/> class. 
        /// </summary>
        /// <param name="configurationReader">
        /// The XML reader.
        /// </param>
        /// <remarks>
        /// The nhs-configuration.xsd is applied to the XML.
        /// </remarks>
        /// <exception cref="SearchConfigurationException">
        /// When nhs-configuration.xsd can't be applied.
        /// </exception>
        public NHSConfigCollection(XmlReader configurationReader)
        {
            XPathNavigator nav;
            try
            {
                nav = new XPathDocument(XmlReader.Create(configurationReader, GetSettings())).CreateNavigator();
            }
            catch (SearchConfigurationException)
            {
                throw;
            }
            catch (Exception e)
            {
                // Encapsule and reThrow
                throw new SearchConfigurationException(e);
            }

            Parse(nav);
        }

        /// <summary>
        /// Gets a value indicating whether HasDefaultConfiguration.
        /// </summary>
        public bool HasDefaultConfiguration
        {
            get
            {
                // If there is no default configuration then return false
                return DefaultConfiguration != null;
            }
        }

        /// <summary>
        /// Gets DefaultConfiguration.
        /// </summary>
        public INHSConfiguration DefaultConfiguration
        {
            get
            {
                // Return if it exists with an empty key.
                if (ContainsKey(string.Empty))
                {
                    return this[string.Empty];
                }

                // Default doesn't exist, return null
                return null;
            }
        }

        /// <summary>
        /// The get configuration.
        /// </summary>
        /// <param name="sessionFactoryName">
        /// The session factory name.
        /// </param>
        /// <returns>
        /// Named configuration or the default if not found.
        /// </returns>
        public INHSConfiguration GetConfiguration(string sessionFactoryName)
        {
            return ContainsKey(sessionFactoryName) ? this[sessionFactoryName] : DefaultConfiguration;
        }

        /// <summary>
        /// The get settings.
        /// </summary>
        /// <returns>
        /// </returns>
        private XmlReaderSettings GetSettings()
        {
            XmlReaderSettings xmlrs = CreateConfigReaderSettings();
            return xmlrs;
        }

        /// <summary>
        /// The create config reader settings.
        /// </summary>
        /// <returns>
        /// </returns>
        private XmlReaderSettings CreateConfigReaderSettings()
        {
            XmlReaderSettings result = CreateXmlReaderSettings(configSchema);
            result.ValidationEventHandler += ConfigSettingsValidationEventHandler;
            result.IgnoreComments = true;
            return result;
        }

        /// <summary>
        /// The create xml reader settings.
        /// </summary>
        /// <param name="xmlSchema">
        /// The xml schema.
        /// </param>
        /// <returns>
        /// </returns>
        private static XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas.Add(xmlSchema);
            return settings;
        }

        /// <summary>
        /// The read xml schema from embedded resource.
        /// </summary>
        /// <param name="resourceName">
        /// The resource name.
        /// </param>
        /// <returns>
        /// </returns>
        private static XmlSchema ReadXmlSchemaFromEmbeddedResource(string resourceName)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            using (Stream resourceStream = executingAssembly.GetManifestResourceStream(resourceName))
            {
                return XmlSchema.Read(resourceStream, null);
            }
        }

        /// <summary>
        /// The config settings validation event handler.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        /// <exception cref="SearchConfigurationException">
        /// </exception>
        private static void ConfigSettingsValidationEventHandler(object sender, ValidationEventArgs e)
        {
            throw new SearchConfigurationException("An exception occurred parsing configuration :" + e.Message, e.Exception);
        }

        /// <summary>
        /// The parse.
        /// </summary>
        /// <param name="navigator">
        /// The navigator.
        /// </param>
        /// <exception cref="AmbiguousSearchCfgException">
        /// </exception>
        private void Parse(XPathNavigator navigator)
        {
            XPathNodeIterator xpni = navigator.Select(CfgXmlHelper.SearchFactoryExpression);
            while (xpni.MoveNext())
            {
                XPathNavigator pNav = xpni.Current.Clone();
                NHSConfiguration config = new NHSConfiguration(pNav);

                // Check to see if we already have a search factory configuration for this session factory instance.
                if (ContainsKey(config.SessionFactoryName))
                {
                    throw new AmbiguousSearchCfgException(
                            "Ambiguous sessionFactoryName. Please specify only one search factory per NHbernate session factory");
                }

                Add(config.SessionFactoryName, config);
            }
        }
    }
}