using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests {
    [Indexed(Index = "Documents")]
    public class DocumentTop {
        private int id;
        private string title;
        private string summary;
        private string text;

        public DocumentTop() {}

        public DocumentTop(string title, string summary, string text) {
            this.title = title;
            this.summary = summary;
            this.text = text;
        }

        [DocumentId]
        public virtual int Id {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        [Boost(2)]
        public virtual string Title {
            get { return title; }
            set { title = value; }
        }

        [Field(Index.Tokenized, Name = "Abstract", Store = Attributes.Store.No)]
        public virtual string Summary {
            get { return summary; }
            set { summary = value; }
        }

        [Field(Index.Tokenized, Store = Attributes.Store.No)]
        public virtual string Text {
            get { return text; }
            set { text = value; }
        }
    }
}