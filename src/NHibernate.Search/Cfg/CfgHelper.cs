using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml;

namespace NHibernate.Search.Cfg {
	public static class CfgHelper {
		private static readonly string configFilePath = GetDefaultConfigurationFilePath();

		/// <summary>
		/// Return NHibernate.Search Configuration. Loaded using the <c>&lt;nhs-configuration&gt;</c> section
		/// from the application config file, if found, or the file <c>nhsearch.cfg.xml</c> if the
		/// <c>&lt;nhs-configuration&gt;</c> section is not present.
		/// If neither is present, then a blank NHSConfiguration is returned.
		/// </summary>
		public static INHSConfiguration LoadConfiguration() {
			INHSConfiguration nhshc = ConfigurationManager.GetSection(CfgXmlHelper.CfgSectionName) as INHSConfiguration;
			if (nhshc != null)
				return nhshc;
			else if (ConfigFileExists(configFilePath))
				using (XmlTextReader reader = new XmlTextReader(configFilePath))
					return new NHSConfiguration(reader);
			else
				return new NHSConfiguration();
		}

		private static bool ConfigFileExists(string filename) {
			return File.Exists(filename);
		}

		private static string GetDefaultConfigurationFilePath() {
			string baseDir = AppDomain.CurrentDomain.BaseDirectory;
			string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
			string binPath = relativeSearchPath == null ? baseDir : Path.Combine(baseDir, relativeSearchPath);
			return Path.Combine(binPath, CfgXmlHelper.DefaultNHSCfgFileName);
		}

		public static void Config(NHibernate.Cfg.Configuration nhCfg) {
			//Todo: add there will be more nhsConfiguration to get
			INHSConfiguration configuration = LoadConfiguration();
			foreach (KeyValuePair<string, string> pair in configuration.Properties) {
				nhCfg.Properties.Add(pair.Key, pair.Value);
			}
		}
	}
}