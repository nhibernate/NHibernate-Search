using System.Collections;
using NHibernate.Search.Backend;
using NHibernate.Search.Backend;
using NHibernate.Search.Backend.Impl.Lucene;
using NHibernate.Search.Engine;
using NHibernate.Search.Store;
using NUnit.Framework;

namespace NHibernate.Search.Tests.LuceneWorkerFixture
{
    [TestFixture]
    public class LuceneWorkerFixture : SearchTestCase
    {
        protected override IList Mappings
        {
            get { return new string[] {"LuceneWorker.Document.hbm.xml"}; }
        }

        /// <summary>
        /// Test purgation of a index.
        /// </summary>
        [Test]
        public void PurgeAll()
        {
            using (ISession s = OpenSession())
            {
                SearchFactory searchFactory = SearchFactory.GetSearchFactory(cfg);
                System.Type targetType = typeof(Document);
                IDirectoryProvider Directory = searchFactory.GetDirectoryProvider(targetType);
                Workspace workspace = new Workspace(searchFactory);


                using (ITransaction tx = s.BeginTransaction())
                {
                    Document doc = new Document("Hibernate in Action", "Object and Relational", "blah blah blah");
                    searchFactory.PerformWork(doc, 1, s, WorkType.Add);
                    doc = new Document("Object and Relational", "Hibernate in Action", "blah blah blah");
                    searchFactory.PerformWork(doc, 2, s, WorkType.Add);
                    tx.Commit();
                }
                Assert.AreEqual(2, workspace.GetIndexReader(targetType).NumDocs(), "Documents created");

                using (ITransaction tx = s.BeginTransaction())
                {
                    LuceneWorker luceneWorker = new LuceneWorker(workspace);
                    luceneWorker.PerformWork(new PurgeAllLuceneWork(targetType), Directory);
                }
                Assert.AreEqual(0, workspace.GetIndexReader(targetType).NumDocs(), "Document purgation");
            }
        }
    }
}