using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Sessions {
    [Indexed]
    public class Email {
        private string body;
        private string header;
        private int id;
        private string title;

        public Email() {}

        public Email(int id, string title, string body) {
            this.id = id;
            this.title = title;
            this.body = body;
        }

        [DocumentId]
        public virtual int Id {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.Tokenized)]
        public virtual string Title {
            get { return title; }
            set { title = value; }
        }

        [Field(Index.Tokenized)]
        public virtual string Body {
            get { return body; }
            set { body = value; }
        }

        public virtual string Header {
            get { return header; }
            set { header = value; }
        }
    }
}