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
        private readonly IDictionary<string, string> properties = new Dictionary<string, string>();
        private string sessionFactoryName = string.Empty;
        
        #region INHSConfiguration Members

        public IDictionary<string, string> Properties {
            get { return properties; }
        }
      
        public string SessionFactoryName {
          get { return sessionFactoryName; }
        }

        public string GetProperty(string name) {
            if (properties.ContainsKey(name))
                return properties[name];

            return null;
        }

        public NHSConfiguration() {}
      
        public NHSConfiguration(XPathNavigator navigator) {
          Parse(navigator);
        }
      
        #endregion

        protected virtual void Parse(XPathNavigator navigator) {
          sessionFactoryName = navigator.GetAttribute(CfgXmlHelper.SessionFactoryNameAttribute, "");
          XPathNodeIterator xpni = navigator.SelectChildren(CfgXmlHelper.PropertiesNodeName, CfgXmlHelper.CfgSchemaXMLNS);
          while (xpni.MoveNext())
          {
            string propName = xpni.Current.GetAttribute(CfgXmlHelper.PropertyNameAttribute, "");
            string propValue = xpni.Current.Value;
            
            if (!string.IsNullOrEmpty(propName) && !string.IsNullOrEmpty(propValue))
              properties[propName] = propValue;
          }
        }
    }
}
