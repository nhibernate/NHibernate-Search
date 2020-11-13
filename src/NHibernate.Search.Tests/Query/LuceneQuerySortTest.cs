using System;
using System.Collections;
using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Query
{
    [TestFixture]
    public class LuceneQuerySortTest : SearchTestCase
    {
        /// <summary>
        /// Test that we can change the default sort order of the lucene search result.
        /// </summary>
        [Test]
        public void TestList()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            CreateTestBooks(s);
            ITransaction tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Summary", new StopAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Summary:lucene");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Book));
            IList result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count, "Wrong number of test results.");

            int id = 1;
            // Ignoring the default sort order as this does not appear to be the same between Lucene.Net and Lucene
            // This also beats against KW's change to queue processing order - see
            /*
            // Make sure that the order is according to in which order the books got inserted into the index.
            foreach (Book b in result)
            {
                Assert.AreEqual(id, b.Id, "Expected another id");
                id++;
            }
            */

            // now the same query, but with a lucene sort specified.
            query = parser.Parse("Summary:lucene");
            hibQuery = s.CreateFullTextQuery(query, typeof(Book));
            Sort sort = new Sort(new SortField("Id", SortFieldType.STRING));
            hibQuery.SetSort(sort);
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count, "Wrong number of test results.");
            id = 3;
            foreach (Book b in result)
            {
                Assert.AreEqual(id, b.Id, "Expected another id");
                id--;
            }

            // order by summary
            query = parser.Parse("Summary:lucene OR Summary:action");
            hibQuery = s.CreateFullTextQuery(query, typeof(Book));
            sort = new Sort(new SortField("summary_forSort", SortFieldType.STRING, true)); //ASC
            hibQuery.SetSort(sort);
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count, "Wrong number of test results.");
            Assert.AreEqual("Groovy in Action", ((Book) result[0]).Summary);

            // order by summary backwards
            query = parser.Parse("Summary:lucene OR Summary:action");
            hibQuery = s.CreateFullTextQuery(query, typeof(Book));
            sort = new Sort(new SortField("summary_forSort", SortFieldType.STRING)); //DESC
            hibQuery.SetSort(sort);
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count, "Wrong number of test results.");
            Assert.AreEqual("Hibernate & Lucene", ((Book) result[0]).Summary);

            // order by date backwards
            query = parser.Parse("Summary:lucene OR Summary:action");
            hibQuery = s.CreateFullTextQuery(query, typeof(Book));
            sort = new Sort(new SortField("PublicationDate", SortFieldType.STRING)); //DESC
            hibQuery.SetSort(sort);
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count, "Wrong number of test results.");
            foreach (Book book in result)
            {
                //System.out.println(book.getSummary() + " : " + book.getPublicationDate());
            }
            Assert.AreEqual("Groovy in Action", ((Book) result[0]).Summary);

            tx.Commit();

            DeleteTestBooks(s);
            s.Close();
        }

        /// <summary>
        /// Helper method creating three books with the same title and summary.
        /// When searching for these books the results should be returned in the order
        /// they got added to the index.
        /// </summary>
        /// <param name="s">The full text session used to index the test data.</param>
        private void CreateTestBooks(IFullTextSession s)
        {
            ITransaction tx = s.BeginTransaction();
            DateTime cal = new DateTime(2007, 07, 25, 11, 20, 30);
            Book book = new Book(1, "Hibernate & Lucene", "This is a test book.");
            book.PublicationDate = cal;
            s.Save(book);
            book = new Book(2, "Hibernate & Lucene", "This is a test book.");
            book.PublicationDate = cal.AddSeconds(1);
            s.Save(book);
            book = new Book(3, "Hibernate & Lucene", "This is a test book.");
            book.PublicationDate = cal.AddSeconds(2);
            s.Save(book);
            book = new Book(4, "Groovy in Action", "The bible of Groovy");
            book.PublicationDate = cal.AddSeconds(3);
            s.Save(book);
            tx.Commit();
            s.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        private void DeleteTestBooks(IFullTextSession s)
        {
            ITransaction tx = s.BeginTransaction();
            s.Delete("from System.Object");
            tx.Commit();
            s.Clear();
        }

        protected override IList Mappings
        {
            get
            {
                return new string[]
                           {
                               "Query.Book.hbm.xml",
                               "Query.Author.hbm.xml",
                           };
            }
        }
    }
}