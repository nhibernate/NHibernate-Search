using System;
using NHibernate.Search.Attributes;
using Index = NHibernate.Search.Attributes.Index;

namespace NHibernate.Search.Tests.Embedded.DoubleInsert
{
    [Indexed]
    public class Address
    {
        [DocumentId] 
        private long id;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] 
        private string address1;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] 
        private string address2;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] 
        private string town;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] 
        private string county;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] 
        private string country;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)] 
        private string postcode;
        private bool active;
        private DateTime createdOn;
        private DateTime lastUpdatedOn;
        [IndexedEmbedded] 
        private Contact contact;

        #region Constructors

        public Address()
        {            
        }

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

        #endregion

        #region Property methods

        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Address1
        {
            get { return address1; }
            set { address1 = value; }
        }

        public string Address2
        {
            get
            {
                return string.IsNullOrEmpty(address2) ? "N/A" : address2;
            }
            set { address2 = value; }
        }

        public string Town
        {
            get { return town; }
            set { town = value; }
        }

        public string County
        {
            get
            {
                return string.IsNullOrEmpty(county) ? "N/A" : county;
            }
            set { county = value; }
        }

        public string Country
        {
            get { return country; }
            set { country = value; }
        }

        public string Postcode
        {
            get { return postcode; }
            set { postcode = value; }
        }

        public bool Active
        {
            get { return active; }
            set { active = value; }
        }

        public Contact Contact
        {
            get { return contact; }
            set { contact = value; }
        }

        public DateTime CreatedOn
        {
            get { return createdOn; }
            set { createdOn = value; }
        }

        public DateTime LastUpdatedOn
        {
            get { return lastUpdatedOn; }
            set { lastUpdatedOn = value; }
        }

        #endregion

        #region Public methods

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Address that = obj as Address;
            if (that == null)
            {
                return false;
            }

            if (!Equals(Address1, that.Address1))
                return false;
            if (!Equals(Address2, that.Address2))
                return false;
            if (!Equals(County, that.County))
                return false;
            if (!Equals(Town, that.Town))
                return false;
            if (!Equals(Postcode, that.Postcode))
                return false;
            if (!Equals(Contact, that.Contact))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int a = 13;

            a = a * 23 + HashCode(address1);
            a = a * 23 + HashCode(address2);
            a = a * 23 + HashCode(county);
            a = a * 23 + HashCode(town);
            a = a * 23 + HashCode(postcode);
            a = a * 23 + HashCode(contact);

            return a;
        }

        public bool IsValidPostcode()
        {
            return false;
        }

        #endregion

        #region Private methods

        private int HashCode(object o)
        {
            return o == null ? 0 : o.GetHashCode();
        }

        #endregion
    }
}