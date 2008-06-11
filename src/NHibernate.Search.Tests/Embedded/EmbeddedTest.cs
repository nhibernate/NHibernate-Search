using System.Collections;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers;
using NHibernate.Cfg;
using NUnit.Framework;

namespace NHibernate.Search.Tests.Embedded
{
    [TestFixture]
    public class EmbeddedTest : SearchTestCase
    {
        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            // TODO: Set up listeners!
        }

        protected override IList Mappings
        {
            get { return new string[] 
                { 
                    "Embedded.Tower.hbm.xml", 
                    "Embedded.Address.hbm.xml", 
                    "Embedded.Product.hbm.xml", 
                    "Embedded.Order.hbm.xml",
                    "Embedded.Author.hbm.xml",
                    "Embedded.Country.hbm.xml" 
                }; }
        }
    }
}
