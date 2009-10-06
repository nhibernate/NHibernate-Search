using System;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Filter
{
	[TestFixture]
	public class FilterTest : SearchTestCase
	{
		protected override IList Mappings
		{
			get { return new string[] { "Filter.Driver.hbm.xml" }; }
		}

		private delegate void Method();

		#region Tests

		//// Broke out NamedFilters into multiple tests as it was trying to do too much in one fixture.

		[Test]
		public void ParameterizedFilter()
		{
			CreateData();
			IFullTextSession s = Search.CreateFullTextSession(OpenSession());
			s.Transaction.Begin();
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term("teacher", "andre")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term("teacher", "max")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term("teacher", "aaron")), BooleanClause.Occur.SHOULD);

			IFullTextQuery ftQuery = s.CreateFullTextQuery(query, typeof(Driver));
			ftQuery.EnableFullTextFilter("security").SetParameter("Login", "andre");
			Assert.AreEqual(1, ftQuery.ResultSize, "Should filter to limit to Emmanuel");

			s.Transaction.Commit();
			s.Close();
			DeleteData();
		}

		[Test]
		public void CombinedFilters()
		{
			CreateData();
			IFullTextSession s = Search.CreateFullTextSession(OpenSession());
			s.Transaction.Begin();
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term("teacher", "andre")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term("teacher", "max")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term("teacher", "aaron")), BooleanClause.Occur.SHOULD);

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

			s.Transaction.Commit();
			s.Close();
			DeleteData();
		}
		
		[Test]
		public void Cache()
		{
			CreateData();
			IFullTextSession s = Search.CreateFullTextSession(OpenSession());
			s.Transaction.Begin();
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term("teacher", "andre")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term("teacher", "max")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term("teacher", "aaron")), BooleanClause.Occur.SHOULD);

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

			s.Transaction.Commit();
			s.Close();
			DeleteData();
		}

		[Test]
		public void StraightFilters()
		{
			CreateData();
			IFullTextSession s = Search.CreateFullTextSession(OpenSession());
			s.Transaction.Begin();
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term("teacher", "andre")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term("teacher", "max")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term("teacher", "aaron")), BooleanClause.Occur.SHOULD);

			IFullTextQuery ftQuery = s.CreateFullTextQuery(query, typeof(Driver));
			ftQuery.EnableFullTextFilter("bestDriver");
			Lucene.Net.Search.Filter dateFilter = new RangeFilter("delivery", "2001", "2005", true, true);
			ftQuery.SetFilter(dateFilter);
			Assert.AreEqual(1, ftQuery.ResultSize, "Should select only liz");

			s.Transaction.Commit();
			s.Close();
			DeleteData();
		}

		#endregion

		#region Helper methods

		private void DeleteData()
		{
			ISession s = OpenSession();
			s.Transaction.Begin();
			s.CreateQuery("delete Driver").ExecuteUpdate();
			Search.CreateFullTextSession(s).PurgeAll(typeof(Driver));
			s.Transaction.Commit();
			s.Close();
		}

		private void CreateData()
		{
			ISession s = OpenSession();
			s.Transaction.Begin();
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
			s.Transaction.Commit();
			s.Close();
		}
		#endregion
	}
}