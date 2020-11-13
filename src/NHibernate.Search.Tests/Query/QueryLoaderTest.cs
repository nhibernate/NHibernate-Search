using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Query
{
    using System.Collections;

    using Lucene.Net.Analysis;
    using Lucene.Net.QueryParsers;

    using NUnit.Framework;

    [TestFixture]
    public class QueryLoaderTest : SearchTestCase
    {
        protected override IList Mappings
        {
            get
            {
                return new string[]
                           {
                               "Query.Author.hbm.xml",
                               "Query.Music.hbm.xml",
                           };
            }
        }

        [Test]
        public void EagerCollectionLoad()
        {
            ISession sess = this.OpenSession();
            ITransaction tx = sess.BeginTransaction();

            Music music = new Music();
            music.Title = "Moo Goes The Cow";
            Author author = new Author();
            author.Name = "Moo Cow";
            music.Authors.Add(author);
            sess.Persist(author);

            author = new Author();
            author.Name = "Another Moo Cow";
            music.Authors.Add(author);
            sess.Persist(author);

            author = new Author();
            author.Name = "A Third Moo Cow";
            music.Authors.Add(author);
            sess.Persist(author);

            author = new Author();
            author.Name = "Random Moo Cow";
            music.Authors.Add(author);
            sess.Persist(author);
            sess.Save(music);

            Music music2 = new Music();
            music2.Title = "The Cow Goes Moo";
            author = new Author();
            author.Name = "Moo Cow The First";
            music2.Authors.Add(author);
            sess.Persist(author);

            author = new Author();
            author.Name = "Moo Cow The Second";
            music2.Authors.Add(author);
            sess.Persist(author);

            author = new Author();
            author.Name = "Moo Cow The Third";
            music2.Authors.Add(author);
            sess.Persist(author);

            author = new Author();
            author.Name = "Moo Cow The Fourth";
            music2.Authors.Add(author);
            sess.Persist(author);
            sess.Save(music2);
            tx.Commit();
            sess.Clear();

            IFullTextSession s = Search.CreateFullTextSession(sess);
            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "title", new KeywordAnalyzer());
            Lucene.Net.Search.Query query = parser.Parse("title:moo");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Music));
            IList result = hibQuery.List();
            Assert.AreEqual(2, result.Count, "Should have returned 2 Music");
            music = (Music) result[0];
            Assert.AreEqual(4, music.Authors.Count, "Music 1 should have 4 authors");
            music = (Music) result[1];
            Assert.AreEqual(4, music.Authors.Count, "Music 2 should have 4 authors");

            // cleanup
            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }
    }
}