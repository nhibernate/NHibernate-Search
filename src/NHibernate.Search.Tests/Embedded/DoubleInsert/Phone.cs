using System;

namespace NHibernate.Search.Tests.Embedded.DoubleInsert
{
    using Attributes;

    [Indexed]
    public class Phone
    {
        [DocumentId]
        private long id;

        [Field(Index = Index.Tokenized, Store = Store.Yes)]
        private string number;

        [Field(Index = Index.Tokenized, Store = Store.Yes)]
        private string type;

        private DateTime createdOn;
        private DateTime lastUpdatedOn;

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
            get
            {
                return string.IsNullOrEmpty(type) ? "N/A" : type;
            }
            set { type = value; }
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

        public Contact Contact
        {
            get { return contact; }
            set { contact = value; }
        }
    }
}
