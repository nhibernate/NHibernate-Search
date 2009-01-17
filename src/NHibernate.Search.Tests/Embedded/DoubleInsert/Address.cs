using System;
using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Embedded.DoubleInsert
{
    public class Address
    {
        [DocumentId] private long id;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] private String address1;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] private String address2;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] private String town;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] private String county;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] private String country;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] private String postcode;
        private bool active;
        private DateTime createdOn;
        private DateTime lastUpdatedOn;
        [IndexedEmbedded] private Contact contact;

        public Address(string address1, string address2, string town,
                       string county, string country, string postcode, bool active, Contact contact)
        {
            this.address1 = address1;
            this.address2 = address2;
            this.town = town;
            this.county = county;
            this.country = country;
            this.postcode = postcode;
            this.active = active;
            this.contact = contact;
        }
    }
}