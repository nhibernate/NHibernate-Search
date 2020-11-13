using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Query
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Documents;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;

    using NUnit.Framework;

    [TestFixture]
    public class ProjectionQueryTest : SearchTestCase
    {
        protected override IList Mappings
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
        public void ResultTransformToDelimString()
        {
            IFullTextSession s = Search.CreateFullTextSession(this.OpenSession());
            this.PrepEmployeeIndex(s);

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

            IList result = hibQuery.List();
            Assert.IsTrue(((string)result[0]).StartsWith("1000, Griffin, ITech"), "incorrect transformation");
            Assert.IsTrue(((string)result[1]).StartsWith("1002, Jimenez, ITech"), "incorrect transformation");

            // cleanup
            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        public void ResultTransformMap()
        {
            IFullTextSession s = Search.CreateFullTextSession(this.OpenSession());
            this.PrepEmployeeIndex(s);

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

            IList transforms = hibQuery.List();
            Dictionary<string, object> map = (Dictionary<string, object>)transforms[1];

            Assert.AreEqual("ITech", map["Dept"], "incorrect transformation");
            Assert.AreEqual(1002, map["Id"], "incorrect transformation");
            Assert.IsTrue(map[ProjectionConstants.DOCUMENT] is Document, "incorrect transformation");
            Assert.AreEqual(
                    "1002",
                    ((Document)map[ProjectionConstants.DOCUMENT]).GetField("Id").GetStringValue(),
                    "incorrect transformation");

            // cleanup
            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        public void LuceneObjectsProjectionWithIterate()
        {
            IFullTextSession s = Search.CreateFullTextSession(this.OpenSession());
            this.PrepEmployeeIndex(s);

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

            foreach (object[] projection in hibQuery.Enumerable())
            {
                Assert.IsNotNull(projection);
                counter++;
                Assert.AreEqual("ITech", projection[2], "dept incorrect");
                Assert.AreEqual(projection[3], s.Get<Employee>(projection[0]), "THIS incorrect");
                Assert.AreEqual(1.0F, projection[4], "SCORE incorrect");
                Assert.AreEqual(1.0F, projection[5], "BOOST incorrect");
                Assert.IsTrue(projection[6] is Document, "DOCUMENT incorrect");
                Assert.AreEqual(4, ((Document)projection[6]).Fields.Count, "DOCUMENT size incorrect");
            }
            Assert.AreEqual(4, counter, "incorrect number of results returned");

            // cleanup
            s.Delete("from System.Object");
            tx.Commit();
            s.Close();
        }

        [Test]
        public void LuceneObjectsProjectionWithList()
        {
            IFullTextSession s = Search.CreateFullTextSession(this.OpenSession());
            this.PrepEmployeeIndex(s);

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

            IList result = hibQuery.List();
            Assert.IsNotNull(result);

            object[] projection = (Object[])result[0];
            Assert.IsNotNull(projection);
            Assert.AreEqual(1001, projection[0], "id incorrect");
            Assert.AreEqual("Jackson", projection[1], "last name incorrect");
            Assert.AreEqual("Accounting", projection[2], "dept incorrect");
            Assert.AreEqual("Jackson", ((Employee)projection[3]).Lastname, "THIS incorrect");
            Assert.AreEqual(projection[3], s.Get<Employee>(projection[0]), "THIS incorrect");
            Assert.AreEqual(1.0F, projection[4], "SCORE incorrect");
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

            result = hibQuery.List();
            Assert.IsNotNull(result);

            projection = (object[])result[0];
            Assert.IsNotNull(projection);

            Assert.IsTrue(projection[0] is Document, "DOCUMENT incorrect");
            Assert.AreEqual(4, ((Document)projection[0]).Fields.Count, "DOCUMENT size incorrect");
            Assert.AreEqual(projection[1], s.Get<Employee>(projection[4]), "THIS incorrect");
            Assert.AreEqual(1.0F, projection[2], "SCORE incorrect");
            Assert.IsNull(projection[3], "BOOST not removed");
            Assert.AreEqual(1001, projection[4], "ID incorrect");
            Assert.AreEqual(1001, projection[5], "id incorrect");
            Assert.AreEqual("Jackson", projection[6], "last name incorrect");
            Assert.AreEqual("Accounting", projection[7], "dept incorrect");
            Assert.IsNotNull(projection[8], "Lucene internal doc id");

            // cleanup
            s.Delete("from System.Object");
            tx.Commit();
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

        // Used by scroll test
        private void checkProjectionFirst(object[] projection, ISession s)
        {
            Assert.AreEqual(1000, projection[0], "id incorrect");
            Assert.AreEqual("Griffin", projection[1], "lastname incorrect");
            Assert.AreEqual("ITech", projection[2], "dept incorrect");
            Assert.AreEqual(projection[3], s.Get<Employee>(projection[0]), "THIS incorrect");
            Assert.AreEqual(1.0F, projection[4], "SCORE incorrect");
            Assert.AreEqual(1.0F, projection[5], "BOOST incorrect");
            Assert.IsTrue(projection[6] is Document, "DOCUMENT incorrect");
            Assert.AreEqual(4, ((Document)projection[6]).Fields.Count, "DOCUMENT size incorrect");
            Assert.AreEqual(1000, projection[7], "legacy ID incorrect");
        }

        // Used by scroll test
        private void checkProjectionLast(Object[] projection, ISession s)
        {
            Assert.AreEqual(1004, projection[0], "id incorrect");
            Assert.AreEqual("Whetbrook", projection[1], "lastname incorrect");
            Assert.AreEqual("ITech", projection[2], "dept incorrect");
            Assert.AreEqual(projection[3], s.Get<Employee>(projection[0]), "THIS incorrect");
            Assert.AreEqual(1.0F, projection[4], "SCORE incorrect");
            Assert.AreEqual(1.0F, projection[5], "BOOST incorrect");
            Assert.IsTrue(projection[6] is Document, "DOCUMENT incorrect");
                        Assert.AreEqual(4, ((Document)projection[6]).Fields.Count, "DOCUMENT size incorrect");
            Assert.AreEqual(1004, projection[7], "legacy ID incorrect");
        }

        // Used by scroll test
        private void checkProjection2(Object[] projection, ISession s)
        {
            Assert.AreEqual(1003, projection[0], "id incorrect");
            Assert.AreEqual("Stejskal", projection[1], "lastname incorrect");
            Assert.AreEqual("ITech", projection[2], "dept incorrect");
            Assert.AreEqual(projection[3], s.Get<Employee>(projection[0]), "THIS incorrect");
            Assert.AreEqual(1.0F, projection[4], "SCORE incorrect");
            Assert.AreEqual(1.0F, projection[5], "BOOST incorrect");
            Assert.IsTrue(projection[6] is Document, "DOCUMENT incorrect");
                        Assert.AreEqual(4, ((Document)projection[6]).Fields.Count, "DOCUMENT size incorrect");
            Assert.AreEqual(1003, projection[7], "legacy ID incorrect");
        }

        private void PrepEmployeeIndex(IFullTextSession s)
        {
            ITransaction tx = s.BeginTransaction();
            Employee e1 = new Employee(1000, "Griffin", "ITech");
            s.Save(e1);
            Employee e2 = new Employee(1001, "Jackson", "Accounting");
            s.Save(e2);
            Employee e3 = new Employee(1002, "Jimenez", "ITech");
            s.Save(e3);
            Employee e4 = new Employee(1003, "Stejskal", "ITech");
            s.Save(e4);
            Employee e5 = new Employee(1004, "Whetbrook", "ITech");
            s.Save(e5);

            tx.Commit();
        }

        #endregion
    }
}