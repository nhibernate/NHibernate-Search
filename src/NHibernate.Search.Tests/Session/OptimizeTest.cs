using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Session
{
    using System.Collections;

    using Impl;

    using Lucene.Net.Analysis;
    using Lucene.Net.QueryParsers;

    using NUnit.Framework;

    [TestFixture]
    public class OptimizeTest : PhysicalTestCase
    {
        protected override IList Mappings
        {
            get { return new string[] { "Session.Email.hbm.xml" }; }
        }

        [Test]
        public void Optimize()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            int loop = 2000;
            for (int i = 0; i < loop; i++)
            {
                s.Persist(new Email(i + 1, "JBoss World Berlin", "Meet the guys who wrote the software"));
            }

            tx.Commit();
            s.Close();

            s = Search.CreateFullTextSession(OpenSession());
            tx = s.BeginTransaction();
            s.SearchFactory.Optimize(typeof(Email));
            tx.Commit();
            s.Close();

            // Check non-indexed object get indexed by s.index;
            s = new FullTextSessionImpl(OpenSession());
            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "id", new StopAnalyzer(LuceneVersion.LUCENE_48));
            int result = s.CreateFullTextQuery(parser.Parse("Body:wrote")).List().Count;
            Assert.AreEqual(2000, result);

            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }
    }
}