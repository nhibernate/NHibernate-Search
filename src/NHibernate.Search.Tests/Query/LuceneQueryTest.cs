using System;
using System.Collections;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NHibernate.Criterion;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Query
{
    [TestFixture]
    public class LuceneQueryTest : SearchTestCase
    {
        protected override IList Mappings
        {
            get
            {
                return new string[]
                           {
                               "Query.Author.hbm.xml",
                               "Query.Book.hbm.xml",
                               "Query.AlternateBook.hbm.xml",
                               "Query.Clock.hbm.xml"
                           };
            }
        }

        #region Tests

        [Test]
        [Ignore("Not implemented")]
        public void List()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            s.Save(clock);
            clock = new Clock(2, "Festina");
            s.Save(clock);
            Book book =
                new Book(1, "La chute de la petite reine a travers les yeux de Festina",
                         "La chute de la petite reine a travers les yeux de Festina, blahblah");
            s.Save(book);
            book = new Book(2, "La gloire de mon père", "Les deboires de mon père en vélo");
            s.Save(book);
            tx.Commit();
            s.Clear();
            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "title", new StopAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Summary:noword");
            IQuery hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            IList result = hibQuery.List();
            Assert.AreEqual(0, result.Count);

            query = parser.Parse("Summary:Festina Or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            result = hibQuery.List();
            Assert.AreEqual(2, result.Count, "Query with explicit class filter");

            query = parser.Parse("Summary:Festina Or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query);
            result = hibQuery.List();
            Assert.AreEqual(2, result.Count, "Query with no class filter");
            foreach (Object element in result)
            {
                Assert.IsTrue(NHibernateUtil.IsInitialized(element));
                s.Delete(element);
            }
            s.Flush();
            tx.Commit();

            tx = s.BeginTransaction();
            query = parser.Parse("Summary:Festina Or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query);
            result = hibQuery.List();
            Assert.AreEqual(0, result.Count, "Query with delete objects");

            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        [Ignore("Not implemented")]
        public void ResultSize()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            s.Save(clock);
            clock = new Clock(2, "Festina");
            s.Save(clock);
            Book book =
                new Book(1, "La chute de la petite reine a travers les yeux de Festina",
                         "La chute de la petite reine a travers les yeux de Festina, blahblah");
            s.Save(book);
            book = new Book(2, "La gloire de mon père", "Les deboires de mon père en vélo");
            s.Save(book);
            tx.Commit();
            s.Clear();
            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "title", new StopAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Summary:noword");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            Assert.AreEqual(0, hibQuery.ResultSize);

            query = parser.Parse("Summary:Festina Or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            Assert.AreEqual(2, hibQuery.ResultSize, "Query with explicit class filter");

            query = parser.Parse("Summary:Festina Or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query);
            Assert.AreEqual(2, hibQuery.ResultSize, "Query with no class filter");
            foreach (Object element in hibQuery.List())
            {
                Assert.IsTrue(NHibernateUtil.IsInitialized(element));
                s.Delete(element);
            }
            s.Flush();
            tx.Commit();

            tx = s.BeginTransaction();
            query = parser.Parse("Summary:Festina Or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query);
            Assert.AreEqual(0, hibQuery.ResultSize, "Query with delete objects");

            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        [Ignore("Not implemented")]
        public void FirstMax()
        {
            ISession sess = OpenSession();
            Assert.AreEqual(0, sess.CreateCriteria(typeof(Clock)).List().Count);

            IFullTextSession s = Search.CreateFullTextSession(sess);
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            s.Save(clock);
            clock = new Clock(2, "Festina");
            s.Save(clock);
            Book book =
                new Book(1, "La chute de la petite reine a travers les yeux de Festina",
                         "La chute de la petite reine a travers les yeux de Festina, blahblah");
            s.Save(book);
            book = new Book(2, "La gloire de mon père", "Les deboires de mon père en vélo");
            s.Save(book);
            tx.Commit();
            s.Clear();
            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "title", new StopAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Summary:Festina Or Brand:Seiko");
            IQuery hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            hibQuery.SetFirstResult(1);
            IList result = hibQuery.List();
            Assert.AreEqual(1, result.Count, "first result no max result");

            hibQuery.SetFirstResult(0);
            hibQuery.SetMaxResults(1);
            result = hibQuery.List();
            Assert.AreEqual(1, result.Count, "max result set");

            hibQuery.SetFirstResult(0);
            hibQuery.SetMaxResults(3);
            result = hibQuery.List();
            Assert.AreEqual(2, result.Count, "max result out of limit");

            hibQuery.SetFirstResult(2);
            hibQuery.SetMaxResults(3);
            result = hibQuery.List();
            Assert.AreEqual(0, result.Count, "first result out of limit");

            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        [Ignore("Not implemented")]
        public void Iterator()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            s.Save(clock);
            clock = new Clock(2, "Festina");
            s.Save(clock);
            Book book =
                new Book(1, "La chute de la petite reine a travers les yeux de Festina",
                         "La chute de la petite reine a travers les yeux de Festina, blahblah");
            s.Save(book);
            book = new Book(2, "La gloire de mon père", "Les deboires de mon père en vélo");
            s.Save(book);
            tx.Commit(); //post Commit events for lucene
            s.Clear();
            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "title", new StopAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Summary:noword");
            IQuery hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            IEnumerator result = hibQuery.Enumerable().GetEnumerator();
            Assert.IsFalse(result.MoveNext());

            query = parser.Parse("Summary:Festina Or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            result = hibQuery.Enumerable().GetEnumerator();
            int index = 0;
            while (result.MoveNext())
            {
                index++;
                s.Delete(result.Current);
            }
            Assert.AreEqual(2, index);

            tx.Commit();

            tx = s.BeginTransaction();
            query = parser.Parse("Summary:Festina Or Brand:Seiko");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            result = hibQuery.Enumerable().GetEnumerator();

            Assert.IsFalse(result.MoveNext());
            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        [Ignore("Not implemented")]
        public void DefaultFetchSize()
        {            
        }

        [Test]
        [Ignore("Not implemented")]
        public void FetchSizeLargerThanHits()
        {            
        }

        [Test]
        [Ignore("Not implemented")]
        public void FetchSizeDefaultFirstAndMax()
        {            
        }

        [Test]
        [Ignore("Not implemented")]
        public void FetchSizeNonDefaultFirstAndMax()
        {            
        }

        [Test]
        [Ignore("Not implemented")]
        public void FetchSizeNonDefaultFirstAndMaxNoHits()
        {            
        }

        [Test]
        [Ignore("Not implemented")]
        public void Current()
        {            
        }

        [Test]
        [Ignore("Not implemented")]
        public void MultipleEntityPerIndex()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            s.Save(clock);
            Book book =
                new Book(1, "La chute de la petite reine a travers les yeux de Festina",
                         "La chute de la petite reine a travers les yeux de Festina, blahblah");
            s.Save(book);
            AlternateBook alternateBook =
                new AlternateBook(1, "La chute de la petite reine a travers les yeux de Festina");
            s.Save(alternateBook);
            tx.Commit();
            s.Clear();
            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Title", new StopAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Summary:Festina");
            IQuery hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            IList result = hibQuery.List();

            Assert.AreEqual(1, result.Count, "Query with explicit class filter");

            query = parser.Parse("Summary:Festina");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            IEnumerator it = hibQuery.Enumerable().GetEnumerator();
            Assert.IsTrue(it.MoveNext());
            Assert.IsNotNull(it.Current);
            Assert.IsFalse(it.MoveNext());

            query = parser.Parse("Summary:Festina OR Brand:seiko");
            hibQuery = s.CreateFullTextQuery(query, typeof(Clock), typeof(Book));
            hibQuery.SetMaxResults(2);
            result = hibQuery.List();
            Assert.AreEqual(2, result.Count, "Query with explicit class filter and limit");

            query = parser.Parse("Summary:Festina");
            hibQuery = s.CreateFullTextQuery(query);
            result = hibQuery.List();
            Assert.AreEqual(2, result.Count, "Query with no class filter");
            foreach (Object element in result)
            {
                Assert.IsTrue(NHibernateUtil.IsInitialized(element));
                s.Delete(element);
            }
            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        public void Criteria()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Book book = new Book(1, "La chute de la petite reine a travers les yeux de Festina", "La chute de la petite reine a travers les yeux de Festina, blahblah");
            s.Save(book);
            Author emmanuel = new Author();
            emmanuel.Name = "Emmanuel";
            s.Save(emmanuel);
            book.Authors.Add(emmanuel);
            tx.Commit();
            s.Clear();

            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Title", new StopAnalyzer(LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Summary:Festina");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Book));
            IList result = hibQuery.List();
            Assert.NotNull(result);
            Assert.AreEqual(1, result.Count, "Query with no explicit criteria");
            book = (Book) result[0];
            //Assert.IsFalse(NHibernate.IsInitialized(book.Authors), "Association should not be initialized");

            result = s.CreateFullTextQuery(query).SetCriteriaQuery(s.CreateCriteria(typeof(Book)).SetFetchMode("Authors", FetchMode.Join)).List();
            Assert.NotNull(result);
            Assert.AreEqual(1, result.Count, "Query with no explicit criteria");
            book = (Book)result[0];
            //Assert.IsTrue(NHibernate.IsInitialized(book.Authors), "Association should be initialized");
            Assert.AreEqual(1, book.Authors.Count);

            // cleanup
            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        public void UsingCriteriaApi()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Clock clock = new Clock(1, "Seiko");
            s.Save(clock);
            tx.Commit();

            IList list = s.CreateFullTextQuery<Clock>("Brand:seiko")
                .SetCriteriaQuery(s.CreateCriteria(typeof(Clock)).Add(Restrictions.IdEq(1)))
                .List();
            Assert.AreEqual(1, list.Count, "should get result back from query");

            s.Delete(clock);
            s.Flush();
            s.Close();
        }

        [Test]
        [Ignore("Not implemented")]
        public void ListEmptyHits()
        {            
        }

        [Test]
        [Ignore("Not implemented")]
        public void IterateEmptyHits()
        {            
        }

        #endregion

        #region Helper methods
        #endregion
    }
}