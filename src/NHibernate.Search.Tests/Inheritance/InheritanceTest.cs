using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers.Classic;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Inheritance
{
    [TestFixture]
    public class InheritanceTest : SearchTestCase
    {
        protected override IEnumerable<string> Mappings
        {
            get
            {
                return new string[]
                           {
                               "Inheritance.Animal.hbm.xml",
                               "Inheritance.Mammal.hbm.xml",
                           };
            }
        }

        [Test]
        public void Inheritance()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            ITransaction tx = s.BeginTransaction();
            Animal a = new Animal();
            a.Name = ("Shark Jr");
            s.Save(a);
            Mammal m = new Mammal();
            m.NumberOfLegs = (4);
            m.Name = ("Elephant Jr");
            s.Save(m);
            tx.Commit(); //post commit events for lucene
            s.Clear();
            tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "Name", new StopAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48));

            Lucene.Net.Search.Query query = parser.Parse("Elephant");
            IQuery hibQuery = s.CreateFullTextQuery(query, typeof(Mammal));
            IList result = hibQuery.List();
            Assert.IsNotEmpty(result);
            Assert.AreEqual(1, result.Count, "Query subclass by superclass attribute");

            query = parser.Parse("NumberOfLegs:[4 TO 4]");
            hibQuery = s.CreateFullTextQuery(query, typeof(Animal), typeof(Mammal));
            result = hibQuery.List();
            Assert.IsNotEmpty(result);
            Assert.AreEqual(1, result.Count, "Query subclass by subclass attribute");

            query = parser.Parse("Jr");
            hibQuery = s.CreateFullTextQuery(query, typeof(Animal));
            result = hibQuery.List();
            Assert.IsNotEmpty(result);
            Assert.AreEqual(2, result.Count, "Query filtering on superclass return mapped subclasses");
            foreach (Object managedEntity in result)
                s.Delete(managedEntity);
            tx.Commit();
            s.Close();
        }
    }
}