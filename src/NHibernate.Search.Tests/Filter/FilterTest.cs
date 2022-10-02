using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Version = Lucene.Net.Util.Version;

namespace NHibernate.Search.Tests.Filter
{
	[TestFixture]
	public class FilterTest : SearchTestCase
	{
		protected override IEnumerable<string> Mappings
		{
			get { return new string[] { "Filter.Driver.hbm.xml" }; }
		}

		private delegate void Method();

		#region Tests

		//// Broke out NamedFilters into multiple tests as it was trying to do too much in one fixture.

		[Test]
		public void ParameterizedFilter()
		{
			try
			{
				CreateData();
                using (var s = Search.CreateFullTextSession(OpenSession()))
                using (var t = s.BeginTransaction())
                {
                    BooleanQuery query = new BooleanQuery();
                    query.Add(new TermQuery(new Term("teacher", "andre")), Occur.SHOULD);
                    query.Add(new TermQuery(new Term("teacher", "max")), Occur.SHOULD);
                    query.Add(new TermQuery(new Term("teacher", "aaron")), Occur.SHOULD);

                    IFullTextQuery ftQuery = s.CreateFullTextQuery(query, typeof(Driver));
                    ftQuery.EnableFullTextFilter("security").SetParameter("Login", "andre");
                    Assert.AreEqual(1, ftQuery.ResultSize, "Should filter to limit to Emmanuel");

                    t.Commit();
                    s.Close();
                }
            }
			finally
			{
				DeleteData();
			}
		}

		[Test]
		public void ParameterizedFilterWithSearchQuery()
		{
			try
			{
				const string n = "NoMatch";
				const string y = "Match";

				using (var session = OpenSession())
                using (var t = session.BeginTransaction())
                {
                    var deliveryDate = new DateTime(2000, 1, 1);
                    saveNewDriver(session, 1, n, n, deliveryDate, -1);
                    saveNewDriver(session, 2, y, y, deliveryDate, -1);
                    saveNewDriver(session, 3, y, y, deliveryDate, -1);
                    saveNewDriver(session, 4, n, n, deliveryDate, -1);
                    saveNewDriver(session, 5, y, y, deliveryDate, -1);
                    saveNewDriver(session, 6, n, y, deliveryDate, -1);
                    saveNewDriver(session, 7, n, n, deliveryDate, -1);
                    saveNewDriver(session, 8, y, n, deliveryDate, -1);
                    saveNewDriver(session, 9, y, y, deliveryDate, -1);
                    saveNewDriver(session, 10, n, n, deliveryDate, -1);
                    saveNewDriver(session, 11, y, y, deliveryDate, -1);
                    saveNewDriver(session, 12, n, n, deliveryDate, -1);
                    saveNewDriver(session, 13, n, n, deliveryDate, -1);
                    saveNewDriver(session, 14, n, y, deliveryDate, -1);
                    saveNewDriver(session, 15, y, n, deliveryDate, -1);
                    t.Commit();
                }

                using (var session = OpenSession())
				using (var ftSession = Search.CreateFullTextSession(session))
				{
					var parser = new QueryParser(Version.LUCENE_29, "name", new StandardAnalyzer(Version.LUCENE_29));
					var query = parser.Parse("name:" + y);
					var ftQuery = ftSession.CreateFullTextQuery(query, typeof (Driver));
					ftQuery.EnableFullTextFilter("security").SetParameter("Login", y);
					var results = ftQuery.List();

					var expectedIds = new[] {2, 3, 5, 9, 11};
					var actualIds = results.Cast<Driver>().OrderBy(x => x.Id).Select(x => x.Id);
					Assert.AreEqual(expectedIds, actualIds, "The query should return only drivers where name AND teacher match.");
				}
			}
			finally
			{
				DeleteData();
			}
		}

		[Test]
		[Ignore("Need to implement BestDriversFilter")]
		public void CombinedFilters()
		{
			try
			{
				CreateData();
                using (var s = Search.CreateFullTextSession(OpenSession()))
                using (var t = s.BeginTransaction())
                {
                    BooleanQuery query = new BooleanQuery();
                    query.Add(new TermQuery(new Term("teacher", "andre")), Occur.SHOULD);
                    query.Add(new TermQuery(new Term("teacher", "max")), Occur.SHOULD);
                    query.Add(new TermQuery(new Term("teacher", "aaron")), Occur.SHOULD);

                    IFullTextQuery ftQuery = s.CreateFullTextQuery(query, typeof(Driver));
                    ftQuery.EnableFullTextFilter("bestDriver");
                    ftQuery.EnableFullTextFilter("security").SetParameter("Login", "andre");
                    Assert.AreEqual(1, ftQuery.ResultSize, "Should filter to limit to Emmanuel");

                    ftQuery = s.CreateFullTextQuery(query, typeof(Driver));
                    ftQuery.EnableFullTextFilter("bestDriver");
                    ftQuery.EnableFullTextFilter("security").SetParameter("login", "andre");
                    ftQuery.DisableFullTextFilter("security");
                    ftQuery.DisableFullTextFilter("bestDriver");
                    Assert.AreEqual(3, ftQuery.ResultSize, "Should not filter anymore");

                    t.Commit();
                    s.Close();
                }
            }
			finally
			{
				DeleteData();
			}
		}
		
		[Test]
		[Ignore("Need to implement ExcludeAllFilter")]
		public void Cache()
		{
			try
			{
				CreateData();
				IFullTextSession s = Search.CreateFullTextSession(OpenSession());
				var t = s.BeginTransaction();
				BooleanQuery query = new BooleanQuery();
				query.Add(new TermQuery(new Term("teacher", "andre")), Occur.SHOULD);
				query.Add(new TermQuery(new Term("teacher", "max")), Occur.SHOULD);
				query.Add(new TermQuery(new Term("teacher", "aaron")), Occur.SHOULD);

				IFullTextQuery ftQuery = s.CreateFullTextQuery(query, typeof(Driver));
				Assert.AreEqual(3, ftQuery.ResultSize, "No filter should happen");

				ftQuery = s.CreateFullTextQuery(query, typeof(Driver));
				ftQuery.EnableFullTextFilter("cachetest");
				Assert.AreEqual(0, ftQuery.ResultSize, "Should filter out all");

				ftQuery = s.CreateFullTextQuery(query, typeof(Driver));
				ftQuery.EnableFullTextFilter("cachetest");
				try
				{
					int i = ftQuery.ResultSize;
				}
				catch (NotSupportedException)
				{
					Assert.Fail("Cache does not work");
				}

				t.Commit();
				s.Close();

			}
			finally
			{
				DeleteData();
			}
		}

		[Test]
		[Ignore("Need to implement BestDriversFilter")]
		public void StraightFilters()
		{
			try
			{
				CreateData();
                using (var s = Search.CreateFullTextSession(OpenSession()))
                using (var t = s.BeginTransaction())
                {
                    BooleanQuery query = new BooleanQuery();
                    query.Add(new TermQuery(new Term("teacher", "andre")), Occur.SHOULD);
                    query.Add(new TermQuery(new Term("teacher", "max")), Occur.SHOULD);
                    query.Add(new TermQuery(new Term("teacher", "aaron")), Occur.SHOULD);

                    IFullTextQuery ftQuery = s.CreateFullTextQuery(query, typeof(Driver));
                    ftQuery.EnableFullTextFilter("bestDriver");
                    Lucene.Net.Search.Filter dateFilter = new TermRangeFilter("delivery", "2001", "2005", true, true);
                    ftQuery.SetFilter(dateFilter);
                    Assert.AreEqual(1, ftQuery.ResultSize, "Should select only liz");

                    t.Commit();
                    s.Close();
                }
            }
			finally
			{
				DeleteData();
			}
		}

		#endregion

		#region Helper methods

		private void DeleteData()
		{
			ISession s = OpenSession();
			var t = s.BeginTransaction();
			s.CreateQuery("delete Driver").ExecuteUpdate();
			Search.CreateFullTextSession(s).PurgeAll(typeof(Driver));
			t.Commit();
			s.Close();
		}

		private void CreateData()
		{
			ISession s = OpenSession();
			var t = s.BeginTransaction();
			Driver driver = new Driver();
			driver.Delivery = new DateTime(2006, 10, 11);
			driver.Id = 1;
			driver.Name = "Emmanuel";
			driver.Score = 5;
			driver.Teacher = "andre";
			s.Save(driver);

			driver = new Driver();
			driver.Delivery = new DateTime(2007, 10, 11);
			driver.Id = 2;
			driver.Name = "Gavin";
			driver.Score = 3;
			driver.Teacher = "aaron";
			s.Save(driver);

			driver = new Driver();
			driver.Delivery = new DateTime(2004, 10, 11);
			driver.Id = 3;
			driver.Name = "Liz";
			driver.Score = 5;
			driver.Teacher = "max";
			s.Save(driver);
			t.Commit();
			s.Close();
		}

		private static void saveNewDriver(ISession session, int id, string name, string teacher, DateTime delivery, int score)
		{
			var driver = new Driver
			{
				Id = id,
				Name = name,
				Teacher = teacher,
				Delivery = delivery,
				Score = score
			};
			session.Save(driver);
		}

		#endregion
	}
}