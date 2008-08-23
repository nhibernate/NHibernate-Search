using System.Collections;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerTest : SearchTestCase
    {
        protected override IList Mappings
        {
            get { return new string[] {"Analyzer.MyEntity.hbm.xml"}; }
        }

        [Test]
        public void TestScopedAnalyzers()
        {
            MyEntity en = new MyEntity();
            en.Entity = "Entity";
            en.Field = "Field";
            en.Property = "Property";
            en.Component = new MyComponent();
            en.Component.ComponentProperty = "component property";

            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            s.Save(en);
            s.Flush();
            tx.Commit();

            tx = s.BeginTransaction();

            QueryParser parser = new QueryParser("id", new StandardAnalyzer());
            Lucene.Net.Search.Query luceneQuery = parser.Parse("entity:alarm");
            IFullTextQuery query = s.CreateFullTextQuery(luceneQuery);
            Assert.AreEqual(1, query.ResultSize, "Entity query");

            luceneQuery = parser.Parse("property:cat");
            query = s.CreateFullTextQuery(luceneQuery);
            Assert.AreEqual(1, query.ResultSize, "Property query");

            luceneQuery = parser.Parse("field:energy");
            query = s.CreateFullTextQuery(luceneQuery);
            Assert.AreEqual(1, query.ResultSize, "Field query");

            // TODO: Uncomment once we have embedded components working
            //luceneQuery = parser.Parse("component.componentProperty:noise");
            //query = s.CreateFullTextQuery(luceneQuery);
            //Assert.AreEqual(1, query.ResultSize, "Component query");

            s.Delete(en);
            tx.Commit();

            s.Close();
        }
    }
}