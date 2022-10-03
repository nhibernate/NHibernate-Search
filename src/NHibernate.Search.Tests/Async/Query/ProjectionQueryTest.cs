﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Query
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Documents;
    using Lucene.Net.QueryParsers.Classic;
    using Lucene.Net.Search;

    using NUnit.Framework;
    using System.Threading.Tasks;
    using System.Threading;

    [TestFixture]
    public class ProjectionQueryTestAsync : SearchTestCase
    {
        protected override IEnumerable<string> Mappings
        {
            get { return new string[]
                             {
                                     "Query.Author.hbm.xml",                                      
                                     "Query.Book.hbm.xml", 
                                     "Query.Employee.hbm.xml"
                             }; }
        }

        #region Tests

        //[Test]
        //[Ignore(".NET doesn't have scrollable resultsets")]
        //public void LuceneObjectsProjectionWithScroll()
        //{
        //}

        [Test]
        public async Task ResultTransformToDelimStringAsync()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            await (PrepEmployeeIndexAsync(s));

            s.Clear();
            ITransaction tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Dept", new StandardAnalyzer(LuceneVersion.LUCENE_48));
            Query query = parser.Parse("Dept:ITech");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Employee));
            hibQuery.SetProjection(
                    "Id",
                    "Lastname",
                    "Dept",
                    ProjectionConstants.THIS,
                    ProjectionConstants.SCORE,
                    ProjectionConstants.BOOST,
                    ProjectionConstants.ID);
            hibQuery.SetResultTransformer(new ProjectionToDelimStringResultTransformer());

            var result = await (hibQuery.ListAsync<string>());
            Assert.That(result[3], Does.StartWith("1000, Griffin, ITech"), "incorrect transformation");
            Assert.That(result[2], Does.StartWith("1002, Jimenez, ITech"), "incorrect transformation");

            // cleanup
            await (s.DeleteAsync("from System.Object"));
            await (tx.CommitAsync());
            s.Close();
        }

        [Test]
        public async Task ResultTransformMapAsync()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            await (PrepEmployeeIndexAsync(s));

            s.Clear();
            ITransaction tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Dept", new StandardAnalyzer(LuceneVersion.LUCENE_48));

            Query query = parser.Parse("Dept:ITech");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Employee));
            hibQuery.SetProjection(
                    "Id",
                    "Lastname",
                    "Dept",
                    ProjectionConstants.THIS,
                    ProjectionConstants.SCORE,
                    ProjectionConstants.BOOST,
                    ProjectionConstants.DOCUMENT,
                    ProjectionConstants.ID);
            hibQuery.SetResultTransformer(new ProjectionToMapResultTransformer());

            var transforms = await (hibQuery.ListAsync<Dictionary<string, object>>());
            var map = transforms[2];

            Assert.AreEqual("ITech", map["Dept"], "incorrect transformation");
            Assert.AreEqual(1002, map["Id"], "incorrect transformation");
            Assert.IsTrue(map[ProjectionConstants.DOCUMENT] is Document, "incorrect transformation");
            Assert.AreEqual(
                    "1002",
                    ((Document)map[ProjectionConstants.DOCUMENT]).GetField("Id").GetStringValue(),
                    "incorrect transformation");

            // cleanup
            await (s.DeleteAsync("from System.Object"));
            await (tx.CommitAsync());
            s.Close();
        }

        [Test]
        public async Task LuceneObjectsProjectionWithIterateAsync()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            await (PrepEmployeeIndexAsync(s));

            s.Clear();
            ITransaction tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Dept", new StandardAnalyzer(LuceneVersion.LUCENE_48));

            Query query = parser.Parse("Dept:ITech");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Employee));
            hibQuery.SetProjection(
                    "Id",
                    "Lastname",
                    "Dept",
                    ProjectionConstants.THIS,
                    ProjectionConstants.SCORE,
                    ProjectionConstants.BOOST,
                    ProjectionConstants.DOCUMENT,
                    ProjectionConstants.ID);

            int counter = 0;

            foreach (object[] projection in await (hibQuery.EnumerableAsync()))
            {
                Assert.IsNotNull(projection);
                counter++;
                Assert.AreEqual("ITech", projection[2], "dept incorrect");
                Assert.AreEqual(projection[3], await (s.GetAsync<Employee>(projection[0])), "THIS incorrect");
                Assert.AreEqual(1.0F, projection[4], "SCORE incorrect");
                Assert.AreEqual(1.0F, projection[5], "BOOST incorrect");
                Assert.IsTrue(projection[6] is Document, "DOCUMENT incorrect");
                Assert.AreEqual(4, ((Document)projection[6]).Fields.Count, "DOCUMENT size incorrect");
            }
            Assert.AreEqual(4, counter, "incorrect number of results returned");

            // cleanup
            await (s.DeleteAsync("from System.Object"));
            await (tx.CommitAsync());
            s.Close();
        }

        [Test]
        public async Task LuceneObjectsProjectionWithListAsync()
        {
            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            await (PrepEmployeeIndexAsync(s));

            s.Clear();
            ITransaction tx = s.BeginTransaction();
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "Dept", new StandardAnalyzer(LuceneVersion.LUCENE_48));

            Query query = parser.Parse("Dept:Accounting");
            IFullTextQuery hibQuery = s.CreateFullTextQuery(query, typeof(Employee));
            hibQuery.SetProjection(
                    "Id",
                    "Lastname",
                    "Dept",
                    ProjectionConstants.THIS,
                    ProjectionConstants.SCORE,
                    ProjectionConstants.BOOST,
                    ProjectionConstants.DOCUMENT,
                    ProjectionConstants.ID,
                    ProjectionConstants.DOCUMENT_ID);

            IList result = await (hibQuery.ListAsync());
            Assert.IsNotNull(result);

            object[] projection = (Object[])result[0];
            Assert.IsNotNull(projection);
            Assert.AreEqual(1001, projection[0], "id incorrect");
            Assert.AreEqual("Jackson", projection[1], "last name incorrect");
            Assert.AreEqual("Accounting", projection[2], "dept incorrect");
            Assert.AreEqual("Jackson", ((Employee)projection[3]).Lastname, "THIS incorrect");
            Assert.AreEqual(projection[3], await (s.GetAsync<Employee>(projection[0])), "THIS incorrect");
            Assert.AreEqual(1.91629076f, projection[4], "SCORE incorrect");
            Assert.AreEqual(1.0F, projection[5], "BOOST incorrect");
            Assert.IsTrue(projection[6] is Document, "DOCUMENT incorrect");
            Assert.AreEqual(4, ((Document)projection[6]).Fields.Count, "DOCUMENT size incorrect");
            Assert.AreEqual(1001, projection[7], "ID incorrect");
            Assert.IsNotNull(projection[8], "Lucene internal doc id");

            // Change the projection order and null one
            hibQuery.SetProjection(
                    ProjectionConstants.DOCUMENT,
                    ProjectionConstants.THIS,
                    ProjectionConstants.SCORE,
                    null,
                    ProjectionConstants.ID,
                    "Id",
                    "Lastname",
                    "Dept",
                    ProjectionConstants.DOCUMENT_ID);

            result = await (hibQuery.ListAsync());
            Assert.IsNotNull(result);

            projection = (object[])result[0];
            Assert.IsNotNull(projection);

            Assert.IsTrue(projection[0] is Document, "DOCUMENT incorrect");
            Assert.AreEqual(4, ((Document)projection[0]).Fields.Count, "DOCUMENT size incorrect");
            Assert.AreEqual(projection[1], await (s.GetAsync<Employee>(projection[4])), "THIS incorrect");
            Assert.AreEqual(1.91629076f, projection[2], "SCORE incorrect");
            Assert.IsNull(projection[3], "BOOST not removed");
            Assert.AreEqual(1001, projection[4], "ID incorrect");
            Assert.AreEqual(1001, projection[5], "id incorrect");
            Assert.AreEqual("Jackson", projection[6], "last name incorrect");
            Assert.AreEqual("Accounting", projection[7], "dept incorrect");
            Assert.IsNotNull(projection[8], "Lucene internal doc id");

            // cleanup
            await (s.DeleteAsync("from System.Object"));
            await (tx.CommitAsync());
            s.Close();
        }

        // Implementing these would increase test coverage

        public void ProjectionWithEmbedded()
        {            
        }

        public void ProjectUnstoredField()
        {            
        }

        #endregion
        #region Helpers

        private async Task PrepEmployeeIndexAsync(IFullTextSession s, CancellationToken cancellationToken = default(CancellationToken))
        {
            ITransaction tx = s.BeginTransaction();
            Employee e1 = new Employee(1000, "Griffin", "ITech");
            await (s.SaveAsync(e1, cancellationToken));
            Employee e2 = new Employee(1001, "Jackson", "Accounting");
            await (s.SaveAsync(e2, cancellationToken));
            Employee e3 = new Employee(1002, "Jimenez", "ITech");
            await (s.SaveAsync(e3, cancellationToken));
            Employee e4 = new Employee(1003, "Stejskal", "ITech");
            await (s.SaveAsync(e4, cancellationToken));
            Employee e5 = new Employee(1004, "Whetbrook", "ITech");
            await (s.SaveAsync(e5, cancellationToken));

            await (tx.CommitAsync(cancellationToken));
        }

        #endregion
    }
}