﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.Collections.Generic;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests
{
    using System.Collections;

    using Lucene.Net.Analysis.Core;
    using Lucene.Net.QueryParsers.Classic;

    using NUnit.Framework;

    using Query;
    using System.Threading.Tasks;

    /// <summary>
    /// Tests the PURGE and PURGE_ALL functionality
    /// </summary>
    [TestFixture]
    public class PurgeTestAsync : SearchTestCase
    {
        [Test]
        public async Task PurgeAsync()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            await (s.SaveAsync(clock));
            clock = new Clock(2, "Festina");
            await (s.SaveAsync(clock));
            Book book = new Book(1, "La chute de la petite reine a travers les yeux de Festina", "La chute de la petite reine a travers les yeux de Festina, blahblah");
            await (s.SaveAsync(book));
            book = new Book(2, "La gloire de mon père", "Les deboires de mon père en vélo");
            await (s.SaveAsync(book));
            await (tx.CommitAsync());
            s.Clear();

            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Brand", new StopAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Brand:Seiko");
            IQuery hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            IList results = await (hibQuery.ListAsync());
            Assert.AreEqual(1, results.Count, "incorrect test record");
            Assert.AreEqual(1, ((Clock)results[0]).Id, "incorrect test record");

            s.Purge(typeof(Clock), ((Clock)results[0]).Id);

            await (tx.CommitAsync());

            tx = s.BeginTransaction();

            query = parser.Parse("Brand:Festina or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            results = await (hibQuery.ListAsync());
            Assert.AreEqual(1, results.Count, "incorrect test record count");
            Assert.AreEqual(2, ((Clock)results[0]).Id, "incorrect test record");

            await (s.DeleteAsync("from System.Object"));
            await (tx.CommitAsync());
            s.Close();
        }

        [Test]
        public async Task PurgeAllAsync()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            await (s.SaveAsync(clock));
            clock = new Clock(2, "Festina");
            await (s.SaveAsync(clock));
            clock = new Clock(3, "Longine");
            await (s.SaveAsync(clock));
            clock = new Clock(4, "Rolex");
            await (s.SaveAsync(clock));
            Book book = new Book(1, "La chute de la petite reine a travers les yeux de Festina", "La chute de la petite reine a travers les yeux de Festina, blahblah");
            await (s.SaveAsync(book));
            book = new Book(2, "La gloire de mon père", "Les deboires de mon père en vélo");
            await (s.SaveAsync(book));
            await (tx.CommitAsync());
            s.Clear();

            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Brand", new StopAnalyzer(LuceneVersion.LUCENE_48));
            tx = s.BeginTransaction();
            s.PurgeAll(typeof(Clock));

            await (tx.CommitAsync());

            tx = s.BeginTransaction();

            Lucene.Net.Search.Query query = parser.Parse("Brand:Festina or Brand:Seiko or Brand:Longine or Brand:Rolex");
            IQuery hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            IList results = await (hibQuery.ListAsync());
            Assert.AreEqual(0, results.Count, "class not completely purged");

            query = parser.Parse("Summary:Festina or Summary:gloire");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            results = await (hibQuery.ListAsync());
            Assert.AreEqual(2, results.Count, "incorrect class purged");

            await (s.DeleteAsync("from System.Object"));
            await (tx.CommitAsync());
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
