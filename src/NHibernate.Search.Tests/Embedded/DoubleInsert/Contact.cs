using System;
using System.Collections.Generic;
using System.Text;

namespace NHibernate.Search.Tests.Embedded.DoubleInsert
{
    using Attributes;

    using Iesi.Collections.Generic;

    public class Contact
    {
        private static readonly long serialVersionUID = 1L;

        [DocumentId]
        private long id;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)]
        private string email;
        private DateTime createdOn;
        private DateTime lastUpdateOn;
        [ContainedIn]
        private ISet<Address> addresses;
        [ContainedIn]
        private ISet<Phone> phoneNumbers;
        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)]
        private string notes;

        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        public DateTime CreatedOn
        {
            get { return createdOn; }
            set { createdOn = value; }
        }

        public DateTime LastUpdateOn
        {
            get { return lastUpdateOn; }
            set { lastUpdateOn = value; }
        }

        public ISet<Address> Addresses
        {
            get { return addresses; }
            set { addresses = value; }
        }

        public ISet<Phone> PhoneNumbers
        {
            get { return phoneNumbers; }
            set { phoneNumbers = value; }
        }
    }
}
