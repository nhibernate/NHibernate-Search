namespace NHibernate.Test
{
    using System;
    using System.IO;
    using Cfg;

    public static class TestConfigurationHelper
    {
        public static readonly string hibernateConfigFile;

        static TestConfigurationHelper()
        {
            // Verify if hibernate.cfg.xml exists
            hibernateConfigFile = TestConfigurationHelper.GetDefaultConfigurationFilePath();
        }

        public static string GetDefaultConfigurationFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
            string folder = relativeSearchPath == null ? baseDir : Path.Combine(baseDir, relativeSearchPath);
            while (folder != null)
            {
                string current = Path.Combine(folder, Configuration.DefaultHibernateCfgFileName);
                if (File.Exists(current))
                    return current;
                folder = Path.GetDirectoryName(folder);
            }

            return null;
        }

        /// <summary>
        /// Standar Configuration for tests.
        /// </summary>
        /// <returns>The configuration using merge between App.Config and hibernate.cfg.xml if present.</returns>
        public static Configuration GetDefaultConfiguration()
        {
            Configuration result = new Configuration();
            if (hibernateConfigFile != null)
                result.Configure(hibernateConfigFile);
            return result;
        }
    }
}