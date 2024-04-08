using System.Collections.Generic;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests
{
    using System.Collections;

    using Lucene.Net.Analysis.Core;
    using Lucene.Net.QueryParsers.Classic;

    using NUnit.Framework;

    using Query;

    /// <summary>
    /// Tests the PURGE and PURGE_ALL functionality
    /// </summary>
    [TestFixture]
    public class PurgeTest : SearchTestCase
    {
        [Test]
        public void Purge()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            s.Save(clock);
            clock = new Clock(2, "Festina");
            s.Save(clock);
            Book book = new Book(1, "La chute de la petite reine a travers les yeux de Festina", "La chute de la petite reine a travers les yeux de Festina, blahblah");
            s.Save(book);
            book = new Book(2, "La gloire de mon p�re", "Les deboires de mon p�re en v�lo");
            s.Save(book);
            tx.Commit();
            s.Clear();

            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Brand", new StopAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Brand:Seiko");
            IQuery hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            IList results = hibQuery.List();
            Assert.AreEqual(1, results.Count, "incorrect test record");
            Assert.AreEqual(1, ((Clock)results[0]).Id, "incorrect test record");

            s.Purge(typeof(Clock), ((Clock)results[0]).Id);

            tx.Commit();

            tx = s.BeginTransaction();

            query = parser.Parse("Brand:Festina or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            results = hibQuery.List();
            Assert.AreEqual(1, results.Count, "incorrect test record count");
            Assert.AreEqual(2, ((Clock)results[0]).Id, "incorrect test record");

            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        public void PurgeAll()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            s.Save(clock);
            clock = new Clock(2, "Festina");
            s.Save(clock);
            clock = new Clock(3, "Longine");
            s.Save(clock);
            clock = new Clock(4, "Rolex");
            s.Save(clock);
            Book book = new Book(1, "La chute de la petite reine a travers les yeux de Festina", "La chute de la petite reine a travers les yeux de Festina, blahblah");
            s.Save(book);
            book = new Book(2, "La gloire de mon p�re", "Les deboires de mon p�re en v�lo");
            s.Save(book);
            tx.Commit();
            s.Clear();

            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Brand", new StopAnalyzer(LuceneVersion.LUCENE_48));
            tx = s.BeginTransaction();
            s.PurgeAll(typeof(Clock));

            tx.Commit();

            tx = s.BeginTransaction();

            Lucene.Net.Search.Query query = parser.Parse("Brand:Festina or Brand:Seiko or Brand:Longine or Brand:Rolex");
            IQuery hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            IList results = hibQuery.List();
            Assert.AreEqual(0, results.Count, "class not completely purged");

            query = parser.Parse("Summary:Festina or Summary:gloire");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            results = hibQuery.List();
            Assert.AreEqual(2, results.Count, "incorrect class purged");

            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        protected override IEnumerable<string> Mappings
        {
            get
            {
                return new string[]
                           {
                               "Query.Author.hbm.xml",
                               "Query.Book.hbm.xml",
                               "Query.Clock.hbm.xml",
                           };
            }
        }
    }
}
