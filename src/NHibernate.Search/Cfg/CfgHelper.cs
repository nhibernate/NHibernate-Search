using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml;
using Configuration=NHibernate.Cfg.Configuration;

namespace NHibernate.Search.Cfg
{
    public static class CfgHelper
    {
        public const string DefaultNHSCfgFileName = "nhsearch.cfg.xml";

        private static readonly string configFilePath = GetDefaultConfigurationFilePath();
        private static INHSConfigCollection configCollection;

        /// <summary>
        /// Return NHibernate.Search Configuration. Loaded using the <c>&lt;nhs-configuration&gt;</c> section
        /// from the application config file, if found, or the file <c>nhsearch.cfg.xml</c> if the
        /// <c>&lt;nhs-configuration&gt;</c> section is not present.
        /// If neither is present, then a blank NHSConfiguration is returned.
        /// </summary>
        public static INHSConfigCollection LoadConfiguration()
        {
            INHSConfigCollection nhshc = ConfigurationManager.GetSection(CfgXmlHelper.CfgSectionName) as INHSConfigCollection;
            if (nhshc != null)
            {
                return nhshc;
            }

            if (!ConfigFileExists(configFilePath))
            {
                return new NHSConfigCollection();
            }

            using (XmlTextReader reader = new XmlTextReader(configFilePath))
            {
                return new NHSConfigCollection(reader);
            }
        }

        public static void Configure(Configuration cfg)
        {
            if (configCollection == null)
            {
                configCollection = LoadConfiguration();
            }

            string sessionFactoryName = string.Empty;

            if (cfg.Properties.ContainsKey(NHibernate.Cfg.Environment.SessionFactoryName))
            {
                sessionFactoryName = cfg.Properties[NHibernate.Cfg.Environment.SessionFactoryName];
            }

            INHSConfiguration configuration = configCollection.GetConfiguration(sessionFactoryName);

            if (configuration == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> pair in configuration.Properties)
            {
                cfg.Properties.Add(pair.Key, pair.Value);
            }
        }

        private static bool ConfigFileExists(string filename)
        {
            return File.Exists(filename);
        }

        private static string GetDefaultConfigurationFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
            string binPath = relativeSearchPath == null ? baseDir : Path.Combine(baseDir, relativeSearchPath);
            return Path.Combine(binPath, DefaultNHSCfgFileName);
        }
    }
}