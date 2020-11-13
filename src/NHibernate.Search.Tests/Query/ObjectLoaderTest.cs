using System.Collections;
using System.Data;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Query
{
    [TestFixture]
    public class ObjectLoaderTest : SearchTestCase
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
        public void ObjectNotFound()
        {
            ISession sess = OpenSession();
            ITransaction tx = sess.BeginTransaction();

            Author author = new Author();
            author.Name = "Moo Cow";
            sess.Persist(author);

            tx.Commit();
            sess.Clear();
            IDbCommand statement = sess.Connection.CreateCommand();
            statement.CommandText = "DELETE FROM Author";
            statement.ExecuteNonQuery();

            IFullTextSession s = Search.CreateFullTextSession(sess);
            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "title", new KeywordAnalyzer());
            Lucene.Net.Search.Query query = parser.Parse("name:moo");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Author), typeof(Music));
            IList result = hibQuery.List();
            Assert.AreEqual(0, result.Count, "Should have returned no author");

            foreach (object o in result)
            {
                s.Delete(o);
            }

            tx.Commit();
            s.Close();
        }
    }
}