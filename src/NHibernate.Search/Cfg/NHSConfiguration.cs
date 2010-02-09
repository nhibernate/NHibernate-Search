namespace NHibernate.Search.Cfg
{
    using System.Collections.Generic;
    using System.Xml.XPath;

    /// <summary>
    /// The NHibernate Search configuration implementation.
    /// </summary>
    public class NHSConfiguration : INHSConfiguration
    {
        private readonly IDictionary<string, string> properties = new Dictionary<string, string>();
        private string sessionFactoryName = string.Empty;

        #region INHSConfiguration Members

        /// <summary>
        /// Gets Properties.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return properties; }
        }

        /// <summary>
        /// Gets SessionFactoryName.
        /// </summary>
        public string SessionFactoryName
        {
            get { return sessionFactoryName; }
        }

        /// <summary>
        /// The get property.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The get property.
        /// </returns>
        public string GetProperty(string name)
        {
            if (properties.ContainsKey(name))
            {
                return properties[name];
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NHSConfiguration"/> class.
        /// </summary>
        public NHSConfiguration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NHSConfiguration"/> class.
        /// </summary>
        /// <param name="navigator">
        /// The navigator.
        /// </param>
        public NHSConfiguration(XPathNavigator navigator)
        {
            Parse(navigator);
        }

        #endregion

        /// <summary>
        /// Parses the configuration, producing a property dictionary.
        /// </summary>
        /// <param name="navigator">
        /// The XPath navigator to use.
        /// </param>
        protected virtual void Parse(XPathNavigator navigator)
        {
            sessionFactoryName = navigator.GetAttribute(CfgXmlHelper.SessionFactoryNameAttribute, string.Empty);
            XPathNodeIterator xpni = navigator.SelectChildren(CfgXmlHelper.PropertiesNodeName, CfgXmlHelper.CfgSchemaXMLNS);
            while (xpni.MoveNext())
            {
                string propName = xpni.Current.GetAttribute(CfgXmlHelper.PropertyNameAttribute, string.Empty);
                string propValue = xpni.Current.Value;

                if (!string.IsNullOrEmpty(propName) && !string.IsNullOrEmpty(propValue))
                {
                    properties[propName] = propValue;
                }
            }
        }
    }
}