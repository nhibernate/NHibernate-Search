using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NUnit.Framework;

namespace NHibernate.Search.Tests.FieldAccess
{
    [TestFixture]
    public class FieldAccessTest : SearchTestCase
    {
        protected override bool RunFixtureSetUpAndTearDownForEachTest => true;

        protected override IEnumerable<string> Mappings
        {
            get
            {
                return new[] { "FieldAccess.Document.hbm.xml" };
            }
        }

        [Test]
        public void FieldBoost()
        {
            ISession s = this.OpenSession();
            ITransaction tx = s.BeginTransaction();
            s.Save(new Document("Hibernate in Action", "Object and Relational", "blah blah blah"));
            s.Save(new Document("Object and Relational", "Hibernate in Action", "blah blah blah"));
            tx.Commit();

            s.Clear();

            IFullTextSession session = Search.CreateFullTextSession(s);
            tx = session.BeginTransaction();
            QueryParser p = new QueryParser(LuceneVersion.LUCENE_48, "id", new StandardAnalyzer(LuceneVersion.LUCENE_48));
            IList result = session.CreateFullTextQuery(p.Parse("title:Action OR Abstract:Action")).List();
            Assert.AreEqual(2, result.Count, "Query by field");
            Assert.AreEqual("Hibernate in Action", ((Document)result[0]).Title, "@Boost fails");
            s.Delete(result[0]);
            s.Delete(result[1]);
            tx.Commit();
            s.Close();
        }

        [Test]
        public void Fields()
        {
            Document doc = new Document(
                    "Hibernate in Action", "Object/relational mapping with Hibernate", "blah blah blah");
            ISession s = this.OpenSession();
            ITransaction tx = s.BeginTransaction();
            s.Save(doc);
            tx.Commit();

            s.Clear();

            IFullTextSession session = Search.CreateFullTextSession(s);
            tx = session.BeginTransaction();
            QueryParser p = new QueryParser(LuceneVersion.LUCENE_48, "id", new StandardAnalyzer(LuceneVersion.LUCENE_48));
            IList result = session.CreateFullTextQuery(p.Parse("Abstract:Hibernate")).List();
            Assert.AreEqual(1, result.Count, "Query by field");
            s.Delete(result[0]);
            tx.Commit();
            s.Close();
        }
    }
}