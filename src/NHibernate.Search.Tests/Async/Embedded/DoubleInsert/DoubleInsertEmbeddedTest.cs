﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;

using NUnit.Framework;

namespace NHibernate.Search.Tests.Embedded.DoubleInsert
{
    using System.Threading.Tasks;
    /// <summary>
    /// The double insert embedded test.
    /// </summary>
    [TestFixture]
    public class DoubleInsertEmbeddedTestAsync : SearchTestCase
    {
        #region Helper methods

        // protected override void Configure(Configuration configuration)
        // {
        // base.Configure(configuration);
        // // TODO: Set up listeners!
        // }

        /// <summary>
        /// Gets Mappings.
        /// </summary>
        protected override IEnumerable<string> Mappings
        {
            get
            {
                return new[]
                           {
                            "Embedded.DoubleInsert.Address.hbm.xml",
                            "Embedded.DoubleInsert.Contact.hbm.xml",
                            "Embedded.DoubleInsert.PersonalContact.hbm.xml",
                            "Embedded.DoubleInsert.BusinessContact.hbm.xml",
                            "Embedded.DoubleInsert.Phone.hbm.xml"
                           };
            }
        }

        #endregion

        #region Tests 

        /// <summary>
        /// The double insert.
        /// </summary>
        [Test]
        public async Task DoubleInsertAsync()
        {
            Address address = new Address();
            address.Address1 = "TEST1";
            address.Address2 = "N/A";
            address.Town = "TEST TOWN";
            address.County = "TEST COUNTY";
            address.Country = "UK";
            address.Postcode = "XXXXXXX";
            address.Active = true;
            address.CreatedOn = DateTime.Now;
            address.LastUpdatedOn = DateTime.Now;

            Phone phone = new Phone();
            phone.Number = "01273234122";
            phone.Type = "HOME";
            phone.CreatedOn = DateTime.Now;
            phone.LastUpdatedOn = DateTime.Now;

            PersonalContact contact = new PersonalContact();
            contact.Firstname = "Amin";
            contact.Surname = "Mohammed-Coleman";
            contact.Email = "address@hotmail.com";
            contact.DateOfBirth = DateTime.Now;

            // contact.NotifyBirthDay( false );
            contact.CreatedOn = DateTime.Now;
            contact.LastUpdatedOn = DateTime.Now;
            contact.Notes = "TEST";
            contact.AddAddressToContact(address);
            contact.AddPhoneToContact(phone);

            IFullTextSession s = Search.CreateFullTextSession(OpenSession());
            var tx = s.BeginTransaction();
            await (s.SaveAsync(contact));
            await (tx.CommitAsync());

            s.Close();

            s = Search.CreateFullTextSession(OpenSession());
            tx = s.BeginTransaction();
            Term term = new Term("county", "county");
            TermQuery termQuery = new TermQuery(term);
            IList results = await (s.CreateFullTextQuery(termQuery).ListAsync());
            Assert.AreEqual(1, results.Count);
            await (s.FlushAsync());
            s.Clear();

            await (s.DeleteAsync("from System.Object"));
            await (tx.CommitAsync());

            s.Close();
        }

        #endregion
    }
}