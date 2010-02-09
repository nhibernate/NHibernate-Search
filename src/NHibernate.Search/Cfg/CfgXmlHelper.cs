using System.Xml;
using System.Xml.XPath;

namespace NHibernate.Search.Cfg 
{
    /// <summary>
    /// Helper to parse nhv-configuration XmlNode.
    /// </summary>
    public static class CfgXmlHelper 
    {
        public const string CfgNamespacePrefix = "cfg";

        /// <summary>The XML Namespace for the nhibernate-configuration</summary>
        public const string CfgSchemaXMLNS = "urn:nhs-configuration-1.0";

        /// <summary>
        /// The XML node name for hibernate configuration section in the App.config/Web.config and
        /// for the hibernate.cfg.xml .
        /// </summary>
        public const string CfgSectionName = "nhs-configuration";

        public static readonly XPathExpression SearchFactoryExpression;

        public static readonly string SessionFactoryNameAttribute = "sessionFactoryName";
        public static readonly string PropertyNameAttribute = "name";
        public static readonly string PropertiesNodeName = "property";

        private const string RootPrefixPath = "//" + CfgNamespacePrefix;

        private static readonly XmlNamespaceManager nsMgr;
     
        static CfgXmlHelper() 
        {
            NameTable nt = new NameTable();
            nsMgr = new XmlNamespaceManager(nt);
            nsMgr.AddNamespace(CfgNamespacePrefix, CfgSchemaXMLNS);

            SearchFactoryExpression = XPathExpression.Compile(RootPrefixPath + ":search-factory", nsMgr);
        }
    }
}
