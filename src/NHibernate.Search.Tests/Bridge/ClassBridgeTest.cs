using Lucene.Net.Analysis.Core;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Bridge
{
    using System.Collections;

    using Lucene.Net.Analysis;
    using Lucene.Net.Documents;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;

    using NHibernate.Cfg;

    using NUnit.Framework;

    /// <summary>
    /// This tests that a field created by a user-supplied
    /// EquipmentType class has been created and is a translation
    /// from an identifier to a manufacturer name.
    /// </summary>
    [TestFixture]
    public class ClassBridgeTest : SearchTestCase
    {
        protected override IList Mappings
        {
            get { return new string[] {"Bridge.Department.hbm.xml", "Bridge.Departments.hbm.xml"}; }
        }

        #region Tests

        /// <summary>
        /// This test checks for two fields being concatentated by the user-supplied
        /// <see cref="CatFieldsClassBridge" /> class which is specified as the implementation class
        /// in the ClassBridge annotation of the <see cref="Department" /> class.
        /// </summary>
        [Test]
        public void ClassBridge()
        {
            ISession s = this.OpenSession();
            ITransaction tx = s.BeginTransaction();
            s.Save(this.getDept1());
            s.Save(this.getDept2());
            s.Save(this.getDept3());
            s.Flush();
            tx.Commit();

            tx = s.BeginTransaction();
            IFullTextSession session = Search.CreateFullTextSession(s);

            // The branchnetwork field is the concatenation of both
            // the branch field and the network field of the Department
            // class. This is in the Lucene document but not in the
            // Department entity itself.
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "branchnetwork", new SimpleAnalyzer(LuceneVersion.LUCENE_48));

            Query query = parser.Parse("branchnetwork:layton 2B");
            IFullTextQuery hibQuery = session.CreateFullTextQuery(query, typeof(Department));
            IList result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.AreEqual("2B", ((Department)result[0]).Network, "incorrect entity returned, wrong network");
            Assert.AreEqual("Layton", ((Department)result[0]).Branch, "incorrect entity returned, wrong branch");
            Assert.AreEqual(1, result.Count, "incorrect number of results returned");

            // Partial match.
            query = parser.Parse("branchnetwork:3c");
            hibQuery = session.CreateFullTextQuery(query, typeof(Department));
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.AreEqual("3C", ((Department)result[0]).Network, "incorrect entity returned, wrong network");
            Assert.AreEqual("West Valley", ((Department)result[0]).Branch, "incorrect entity returned, wrong branch");
            Assert.AreEqual(1, result.Count, "incorrect number of results returned");

            // No data cross-ups .
            query = parser.Parse("branchnetwork:Kent Lewin");
            hibQuery = session.CreateFullTextQuery(query, typeof(Department));
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count == 0, "problem with field cross-ups");

            // Non-ClassBridge field.
            parser = new QueryParser(LuceneVersion.LUCENE_48, "BranchHead", new SimpleAnalyzer(LuceneVersion.LUCENE_48));
            query = parser.Parse("BranchHead:Kent Lewin");
            hibQuery = session.CreateFullTextQuery(query, typeof(Department));
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count == 1, "incorrect entity returned, wrong branch head");
            Assert.AreEqual("Kent Lewin", ((Department)result[0]).BranchHead, "incorrect entity returned");

            // Cleanup
            foreach (object element in s.CreateQuery("from " + typeof(Department).FullName).List())
            {
                s.Delete(element);
            }
            tx.Commit();
            s.Close();
        }

        /// <summary>
        /// This tests that a field created by a user-supplied
        /// EquipmentType class has been created and is a translation
        /// from an identifier to a manufacturer name.
        /// </summary>
        [Test]
        public void ClassBridges()
        {
            ISession s = this.OpenSession();
            ITransaction tx = s.BeginTransaction();
            s.Save(this.getDepts1());
            s.Save(this.getDepts2());
            s.Save(this.getDepts3());
            s.Save(this.getDepts4());
            s.Flush();
            tx.Commit();

            tx = s.BeginTransaction();
            IFullTextSession session = Search.CreateFullTextSession(s);

            // The equipment field is the manufacturer field  in the
            // Departments entity after being massaged by passing it
            // through the EquipmentType class. This field is in
            // the Lucene document but not in the Department entity itself.
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "equipment", new SimpleAnalyzer(LuceneVersion.LUCENE_48));

            // Check the second ClassBridge annotation
            Query query = parser.Parse("equiptype:Cisco");
            IFullTextQuery hibQuery = session.CreateFullTextQuery(query, typeof(Departments));
            IList result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count, "incorrect number of results returned");
            foreach (Departments d in result)
            {
                Assert.AreEqual("C", d.Manufacturer, "incorrect manufacturer");
            }

            // No data cross-ups.
            query = parser.Parse("branchnetwork:Kent Lewin");
            hibQuery = session.CreateFullTextQuery(query, typeof(Departments));
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count == 0, "problem with field cross-ups");

            // Non-ClassBridge field.
            parser = new QueryParser(LuceneVersion.LUCENE_48, "BranchHead", new SimpleAnalyzer(LuceneVersion.LUCENE_48));
            query = parser.Parse("BranchHead:Kent Lewin");
            hibQuery = session.CreateFullTextQuery(query, typeof(Departments));
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count == 1, "incorrect entity returned, wrong branch head");
            Assert.AreEqual("Kent Lewin", ((Departments)result[0]).BranchHead, "incorrect entity returned");

            // Check other ClassBridge annotation.
            parser = new QueryParser(LuceneVersion.LUCENE_48, "branchnetwork", new SimpleAnalyzer(LuceneVersion.LUCENE_48));
            query = parser.Parse("branchnetwork:st. george 1D");
            hibQuery = session.CreateFullTextQuery(query, typeof(Departments));
            result = hibQuery.List();
            Assert.IsNotNull(result);
            Assert.AreEqual("1D", ((Departments)result[0]).Network, "incorrect entity returned, wrong network");
            Assert.AreEqual("St. George", ((Departments)result[0]).Branch, "incorrect entity returned, wrong branch");
            Assert.AreEqual(1, result.Count, "incorrect number of results returned");

            // Cleanup
            foreach (object element in s.CreateQuery("from " + typeof(Departments).FullName).List())
            {
                s.Delete(element);
            }
            tx.Commit();
            s.Close();
        }

        /// <summary>
        /// This is the same test as above with a projection query
        /// to show the presence of the ClassBridge impl built fields
        /// just in case you don't believe us.
        /// </summary>
        [Test]
        public void ClassBridgesWithProjection()
        {
            ISession s = this.OpenSession();
            ITransaction tx = s.BeginTransaction();
            s.Save(this.getDepts1());
            s.Save(this.getDepts2());
            s.Save(this.getDepts3());
            s.Save(this.getDepts4());
            s.Flush();
            tx.Commit();

            tx = s.BeginTransaction();
            IFullTextSession session = Search.CreateFullTextSession(s);

            // The equipment field is the manufacturer field  in the
            // Departments entity after being massaged by passing it
            // through the EquipmentType class. This field is in
            // the Lucene document but not in the Department entity itself.
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "equipment", new SimpleAnalyzer(LuceneVersion.LUCENE_48));

            // Check the second ClassBridge annotation
            Query query = parser.Parse("equiptype:Cisco");
            IFullTextQuery hibQuery = session.CreateFullTextQuery(query, typeof(Departments));

            hibQuery.SetProjection(ProjectionConstants.THIS, ProjectionConstants.DOCUMENT);

            IEnumerator projections = hibQuery.Enumerable().GetEnumerator();
            Assert.IsNotNull(projections);

            projections.MoveNext();
            object[] projection = (object[])projections.Current;

            Assert.AreEqual(typeof(Departments), projection[0].GetType(), "DOCUMENT incorrect");
            // Note: This assertion fails when run with other tests because the id is assigned by the database, and previous tests have already used this value
            //Assert.AreEqual(1, ((Departments)projection[0]).Id, "id incorrect");
            Assert.IsTrue(projection[1] is Document, "DOCUMENT incorrect");
                        Assert.AreEqual(8, ((Document)projection[1]).Fields.Count, "DOCUMENT size incorrect");
            Assert.IsNotNull(((Document)projection[1]).GetField("equiptype"), "equiptype is null");
            Assert.AreEqual(
                    "Cisco", ((Document)projection[1]).GetField("equiptype").GetStringValue(), "equiptype incorrect");
            Assert.IsNotNull(((Document)projection[1]).GetField("branchnetwork"), "branchnetwork is null");
            Assert.AreEqual(
                    "Salt Lake City 1A",
                    ((Document)projection[1]).GetField("branchnetwork").GetStringValue(),
                    "branchnetwork incorrect");

            projections.MoveNext();
            projection = (object[])projections.Current;

            Assert.AreEqual(typeof(Departments), projection[0].GetType(), "DOCUMENT incorrect");
            // NB This assertion causes the test to break when run with other tests - some leakage?
            //Assert.AreEqual(4, ((Departments)projection[0]).Id, "id incorrect");
            Assert.IsTrue(projection[1] is Document, "DOCUMENT incorrect");
                        Assert.AreEqual(8, ((Document)projection[1]).Fields.Count, "DOCUMENT size incorrect");
            Assert.IsNotNull(((Document)projection[1]).GetField("equiptype"), "equiptype is null");
            Assert.AreEqual(
                    "Cisco", ((Document)projection[1]).GetField("equiptype").GetStringValue(), "equiptype incorrect");
            Assert.IsNotNull(((Document)projection[1]).GetField("branchnetwork"), "branchnetwork is null");
            Assert.AreEqual(
                    "St. George 1D",
                    ((Document)projection[1]).GetField("branchnetwork").GetStringValue(),
                    "branchnetwork incorrect");

            Assert.AreEqual(false, projections.MoveNext(), "incorrect result count returned");

            //cleanup
            foreach (object element in s.CreateQuery("from " + typeof(Departments).FullName).List())
            {
                s.Delete(element);
            }
            tx.Commit();
            s.Close();
        }

        #endregion

        #region Helper methods

        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            configuration.SetProperty(
                    NHibernate.Search.Environment.AnalyzerClass, typeof(SimpleAnalyzer).AssemblyQualifiedName);
        }

        private Department getDept1()
        {
            Department dept = new Department();

            dept.Branch = "Salt Lake City";
            dept.BranchHead = "Kent Lewin";
            dept.MaxEmployees = 100;
            dept.Network = "1A";
            return dept;
        }

        private Department getDept2()
        {
            Department dept = new Department();

            dept.Branch = "Layton";
            dept.BranchHead = "Terry Poperszky";
            dept.MaxEmployees = 20;
            dept.Network = "2B";

            return dept;
        }

        private Department getDept3()
        {
            Department dept = new Department();

            dept.Branch = "West Valley";
            dept.BranchHead = "Pat Kelley";
            dept.MaxEmployees = 15;
            dept.Network = "3C";

            return dept;
        }

        private Departments getDepts1()
        {
            Departments depts = new Departments();

            depts.Branch = "Salt Lake City";
            depts.BranchHead = "Kent Lewin";
            depts.MaxEmployees = 100;
            depts.Network = "1A";
            depts.Manufacturer = "C";

            return depts;
        }

        private Departments getDepts2()
        {
            Departments depts = new Departments();

            depts.Branch = "Layton";
            depts.BranchHead = "Terry Poperszky";
            depts.MaxEmployees = 20;
            depts.Network = "2B";
            depts.Manufacturer = "3";

            return depts;
        }

        private Departments getDepts3()
        {
            Departments depts = new Departments();

            depts.Branch = "West Valley";
            depts.BranchHead = "Pat Kelley";
            depts.MaxEmployees = 15;
            depts.Network = "3C";
            depts.Manufacturer = "D";

            return depts;
        }

        private Departments getDepts4()
        {
            Departments depts = new Departments();

            depts.Branch = "St. George";
            depts.BranchHead = "Spencer Stajskal";
            depts.MaxEmployees = 10;
            depts.Network = "1D";
            depts.Manufacturer = "C";
            return depts;
        }

        #endregion
    }
}