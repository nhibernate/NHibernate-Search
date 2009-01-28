using System;

namespace NHibernate.Search.Tests.Embedded.DoubleInsert
{
    using Attributes;

    public class Phone
    {
        private long id;

        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)]
        private string number;

        [Field(Index = Index.Tokenized, Store = Attributes.Store.Yes)]
        private string type;

        private DateTime createdOn;

        private DateTime lastUpdateOn;

        [IndexedEmbedded]
        private Contact contact;

        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Number
        {
            get { return number; }
            set { number = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
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

        public Contact Contact
        {
            get { return contact; }
            set { contact = value; }
        }
    }
}
