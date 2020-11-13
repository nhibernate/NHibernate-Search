using System.IO;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using NHibernate.Cfg;
using NHibernate.Search.Store;

namespace NHibernate.Search.Tests
{
    public abstract class PhysicalTestCase : SearchTestCase
    {
        protected FileInfo BaseIndexDir
        {
            get
            {
                FileInfo current = new FileInfo(".");
                FileInfo sub = new FileInfo(current.FullName + "\\indextemp");
                return sub;
            }
        }

        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            DeleteBaseIndexDir();
            FileInfo sub = BaseIndexDir;
            Directory.CreateDirectory(sub.FullName);

            configuration.SetProperty("hibernate.search.default.indexBase", sub.FullName);
            configuration.SetProperty("hibernate.search.default.directory_provider", typeof(FSDirectoryProvider).AssemblyQualifiedName);
            configuration.SetProperty(NHibernate.Search.Environment.AnalyzerClass, typeof(StopAnalyzer).AssemblyQualifiedName);
        }

        protected void DeleteBaseIndexDir()
        {
            FileInfo sub = BaseIndexDir;
            try
            {
                Delete(sub);
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex); // "The process cannot access the file '_0.cfs' because it is being used by another process."
            }
        }

        protected override void OnTearDown()
        {
            base.OnTearDown();
            if (sessions != null)
            {
                sessions.Close(); // Close the files in the indexDir
            }

            DeleteBaseIndexDir();
        }
    }
}
