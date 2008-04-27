using System.Configuration;
using System.Xml;

namespace NHibernate.Search.Cfg {
    public class ConfigurationSectionHandler : IConfigurationSectionHandler {
        #region IConfigurationSectionHandler Members

        public object Create(object parent, object configContext, XmlNode section) {
            XmlTextReader reader = new XmlTextReader(section.OuterXml, XmlNodeType.Document, null);
            return new NHSConfiguration(reader);
        }

        #endregion
    }
}