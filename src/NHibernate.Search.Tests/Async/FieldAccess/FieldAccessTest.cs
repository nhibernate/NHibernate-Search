﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using NUnit.Framework;

namespace NHibernate.Search.Tests.FieldAccess
{
    using System.Threading.Tasks;
    [TestFixture]
    public class FieldAccessTestAsync : SearchTestCase
    {
        protected override bool RunFixtureSetUpAndTearDownForEachTest => true;

        protected override IEnumerable<string> Mappings
        {
            get
            {
                return new[] {"FieldAccess.Document.hbm.xml"};
            }
        }

        [Test]
        public async Task FieldBoostAsync()
        {
            ISession s = this.OpenSession();
            ITransaction tx = s.BeginTransaction();
            await (s.SaveAsync(new Document("Hibernate in Action", "Object and Relational", "blah blah blah")));
            await (s.SaveAsync(new Document("Object and Relational", "Hibernate in Action", "blah blah blah")));
            await (tx.CommitAsync());

            s.Clear();

            IFullTextSession session = Search.CreateFullTextSession(s);
            tx = session.BeginTransaction();
            QueryParser p = new QueryParser(LuceneVersion.LUCENE_48, "id", new StandardAnalyzer(LuceneVersion.LUCENE_48));
            IList result = await (session.CreateFullTextQuery(p.Parse("title:Action OR Abstract:Action")).ListAsync());
            Assert.AreEqual(2, result.Count, "Query by field");
            Assert.AreEqual("Hibernate in Action", ((Document)result[0]).Title, "@Boost fails");
            await (s.DeleteAsync(result[0]));
            await (s.DeleteAsync(result[1]));
            await (tx.CommitAsync());
            s.Close();
        }

        [Test]
        public async Task FieldsAsync()
        {
            Document doc = new Document(
                    "Hibernate in Action", "Object/relational mapping with Hibernate", "blah blah blah");
            ISession s = this.OpenSession();
            ITransaction tx = s.BeginTransaction();
            await (s.SaveAsync(doc));
            await (tx.CommitAsync());

            s.Clear();

            IFullTextSession session = Search.CreateFullTextSession(s);
            tx = session.BeginTransaction();
            QueryParser p = new QueryParser(LuceneVersion.LUCENE_48, "id", new StandardAnalyzer(LuceneVersion.LUCENE_48));
            IList result = await (session.CreateFullTextQuery(p.Parse("Abstract:Hibernate")).ListAsync());
            Assert.AreEqual(1, result.Count, "Query by field");
            await (s.DeleteAsync(result[0]));
            await (tx.CommitAsync());
            s.Close();
        }
    }
}