using System.Collections;
using Lucene.Net.Index;
using NHibernate.Cfg;
using NUnit.Framework;

namespace NHibernate.Search.Tests.IndexingStrategy
{
    [TestFixture]
    public class ManualIndexingStrategyTest : SearchTestCase
    {
        private int GetDocumentNbr()
        {
            IndexReader reader = IndexReader.Open(GetDirectory(typeof(DocumentTop)));
            try
            {
                return reader.NumDocs;
            }
            finally
            {
                reader.Dispose();
            }
        }

        protected override IList Mappings
        {
            get { return new string[] { "DocumentTop.hbm.xml", "AlternateDocument.hbm.xml" }; }
        }

        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            configuration.SetProperty(Environment.IndexingStrategy, "manual");
        }

        [Test]
        public void MultipleEntitiesPerIndex()
        {
            ISession s = OpenSession();
            ITransaction tx = s.BeginTransaction();
            DocumentTop document = new DocumentTop("Hibernate in Action", "Object/relational mapping with Hibernate", "blah blah blah");
            s.Save(document);
            s.Flush();

            int tempAux = document.Id;
            s.Save(new AlternateDocument(tempAux, "Hibernate in Action", "Object/relational mapping with Hibernate", "blah blah blah"));
            tx.Commit();
            s.Close();

            Assert.AreEqual(0, GetDocumentNbr());

            s = OpenSession();
            tx = s.BeginTransaction();
            s.Delete(s.Get(typeof(AlternateDocument), document.Id));
            s.Delete(s.CreateCriteria(typeof(DocumentTop)).UniqueResult());
            tx.Commit();
            s.Close();
        }
    }
}