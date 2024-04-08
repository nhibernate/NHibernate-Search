using System;

namespace NHibernate.Search.Tests.Query
{
    using Attributes;

    [Indexed(Index = "Book")]
    public class AlternateBook
    {
        private int id;
        private String summary;

        public AlternateBook()
        {
        }

        public AlternateBook(int id, string summary)
        {
            this.id = id;
            this.summary = summary;
        }

        [DocumentId]
        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        public virtual string Summary
        {
            get { return summary; }
            set { summary = value; }
        }
    }
}