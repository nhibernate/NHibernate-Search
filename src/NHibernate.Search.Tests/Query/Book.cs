using System;
using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Queries {
    [Indexed(Index = "Book")]
    public class Book {
        private String body;
        private int id;
        private String summary;

        public Book(int id, string body, string summary) {
            this.id = id;
            this.body = body;
            this.summary = summary;
        }

        public Book() {}

        [DocumentId]
        public virtual int Id {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.Tokenized, Store=Attributes.Store.No)]
        public virtual string Body {
            get { return body; }
            set { body = value; }
        }

        [Field(Index.Tokenized, Store=Attributes.Store.Yes)]
        public virtual string Summary {
            get { return summary; }
            set { summary = value; }
        }
    }
}