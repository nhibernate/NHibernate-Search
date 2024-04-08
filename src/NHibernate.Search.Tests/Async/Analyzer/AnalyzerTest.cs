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

namespace NHibernate.Search.Tests.Analyzer
{
    using System.Threading.Tasks;
    [TestFixture]
    public class AnalyzerTestAsync : SearchTestCase
    {
        protected override IEnumerable<string> Mappings
        {
            get { return new string[] { "Analyzer.MyEntity.hbm.xml" }; }
        }

        [Test, Explicit("Broken after 3.0.3 upgrade")]
        public async Task TestScopedAnalyzersAsync()
        {
            MyEntity en = new MyEntity();
            en.Entity = "Entity";
            en.Field = "Field";
            en.Property = "Property";
            en.Component = new MyComponent();
            en.Component.ComponentProperty = "component property";

            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            await (s.SaveAsync(en));
            await (s.FlushAsync());
            await (tx.CommitAsync());

            tx = s.BeginTransaction();

            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "id", new StandardAnalyzer(LuceneVersion.LUCENE_48));
            Lucene.Net.Search.Query luceneQuery = parser.Parse("entity:alarm");
            IFullTextQuery query = s.CreateFullTextQuery(luceneQuery, typeof(MyEntity));
            Assert.AreEqual(1, query.ResultSize, "Entity query");

            luceneQuery = parser.Parse("property:cat");
            query = s.CreateFullTextQuery(luceneQuery, typeof(MyEntity));
            Assert.AreEqual(1, query.ResultSize, "Property query");

            luceneQuery = parser.Parse("field:energy");
            query = s.CreateFullTextQuery(luceneQuery, typeof(MyEntity));
            Assert.AreEqual(1, query.ResultSize, "Field query");

            luceneQuery = parser.Parse("component.componentProperty:noise");
            query = s.CreateFullTextQuery(luceneQuery);
            Assert.AreEqual(1, query.ResultSize, "Component query");

            await (s.DeleteAsync(await (query.UniqueResultAsync())));
            await (tx.CommitAsync());

            s.Close();
        }
    }
}