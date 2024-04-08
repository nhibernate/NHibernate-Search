﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Query
{
    using System.Threading.Tasks;
    using System.Threading;
    [TestFixture]
    public class LuceneQuerySortTestAsync : SearchTestCase
    {
        /// <summary>
        /// Test that we can change the default sort order of the lucene search result.
        /// </summary>
        [Test]
        public async Task TestListAsync()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            await (CreateTestBooksAsync(s));
            ITransaction tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "Summary", new StopAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Summary:lucene");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Book));
            IList result = await (hibQuery.ListAsync());
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
            Sort sort = new Sort(new SortField("Id", SortFieldType.INT32, true));
            hibQuery.SetSort(sort);
            result = await (hibQuery.ListAsync());
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
            sort = new Sort(new SortField("summary_forSort", SortFieldType.STRING, false)); //ASC
            hibQuery.SetSort(sort);
            result = await (hibQuery.ListAsync());
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count, "Wrong number of test results.");
            Assert.AreEqual("Groovy in Action", ((Book)result[0]).Summary);

            // order by summary backwards
            query = parser.Parse("Summary:lucene OR Summary:action");
            hibQuery = s.CreateFullTextQuery(query, typeof(Book));
            sort = new Sort(new SortField("summary_forSort", SortFieldType.STRING, true)); //DESC
            hibQuery.SetSort(sort);
            result = await (hibQuery.ListAsync());
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count, "Wrong number of test results.");
            Assert.AreEqual("Hibernate & Lucene", ((Book)result[0]).Summary);

            // order by date backwards
            query = parser.Parse("Summary:lucene OR Summary:action");
            hibQuery = s.CreateFullTextQuery(query, typeof(Book));
            sort = new Sort(new SortField("PublicationDate", SortFieldType.STRING, true)); //DESC
            hibQuery.SetSort(sort);
            result = await (hibQuery.ListAsync());
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count, "Wrong number of test results.");
            foreach (Book book in result)
            {
                //System.out.println(book.getSummary() + " : " + book.getPublicationDate());
            }
            Assert.AreEqual("Groovy in Action", ((Book)result[0]).Summary);

            await (tx.CommitAsync());

            await (DeleteTestBooksAsync(s));
            s.Close();
        }

        /// <summary>
        /// Helper method creating three books with the same title and summary.
        /// When searching for these books the results should be returned in the order
        /// they got added to the index.
        /// </summary>
        /// <param name="s">The full text session used to index the test data.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        private async Task CreateTestBooksAsync(IFullTextSession s, CancellationToken cancellationToken = default(CancellationToken))
        {
            ITransaction tx = s.BeginTransaction();
            DateTime cal = new DateTime(2007, 07, 25, 11, 20, 30);
            Book book = new Book(1, "Hibernate & Lucene", "This is a test book.");
            book.PublicationDate = cal;
            await (s.SaveAsync(book, cancellationToken));
            book = new Book(2, "Hibernate & Lucene", "This is a test book.");
            book.PublicationDate = cal.AddSeconds(1);
            await (s.SaveAsync(book, cancellationToken));
            book = new Book(3, "Hibernate & Lucene", "This is a test book.");
            book.PublicationDate = cal.AddSeconds(2);
            await (s.SaveAsync(book, cancellationToken));
            book = new Book(4, "Groovy in Action", "The bible of Groovy");
            book.PublicationDate = cal.AddSeconds(3);
            await (s.SaveAsync(book, cancellationToken));
            await (tx.CommitAsync(cancellationToken));
            s.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        private async Task DeleteTestBooksAsync(IFullTextSession s, CancellationToken cancellationToken = default(CancellationToken))
        {
            ITransaction tx = s.BeginTransaction();
            await (s.DeleteAsync("from System.Object", cancellationToken));
            await (tx.CommitAsync(cancellationToken));
            s.Clear();
        }

        protected override IEnumerable<string> Mappings
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