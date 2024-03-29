﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.Collections.Generic;
using Lucene.Net.Index;
using NHibernate.Cfg;
using NUnit.Framework;

namespace NHibernate.Search.Tests.IndexingStrategy
{
    using System.Threading.Tasks;
    [TestFixture]
    public class ManualIndexingStrategyTestAsync : SearchTestCase
    {
        private int GetDocumentNbr()
        {
            var reader = DirectoryReader.Open(GetDirectory(typeof(DocumentTop)));
            try
            {
                return reader.NumDocs;
            }
            finally
            {
                reader.Dispose();
            }
        }

        protected override IEnumerable<string> Mappings
        {
            get { return new string[] { "DocumentTop.hbm.xml", "AlternateDocument.hbm.xml" }; }
        }

        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);
            configuration.SetProperty(Environment.IndexingStrategy, "manual");
        }

        [Test]
        public async Task MultipleEntitiesPerIndexAsync()
        {
            ISession s = OpenSession();
            ITransaction tx = s.BeginTransaction();
            DocumentTop document = new DocumentTop("Hibernate in Action", "Object/relational mapping with Hibernate", "blah blah blah");
            await (s.SaveAsync(document));
            await (s.FlushAsync());

            int tempAux = document.Id;
            await (s.SaveAsync(new AlternateDocument(tempAux, "Hibernate in Action", "Object/relational mapping with Hibernate", "blah blah blah")));
            await (tx.CommitAsync());
            s.Close();

            Assert.AreEqual(0, GetDocumentNbr());

            s = OpenSession();
            tx = s.BeginTransaction();
            await (s.DeleteAsync(await (s.GetAsync(typeof(AlternateDocument), document.Id))));
            await (s.DeleteAsync(await (s.CreateCriteria(typeof(DocumentTop)).UniqueResultAsync())));
            await (tx.CommitAsync());
            s.Close();
        }
    }
}