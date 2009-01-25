using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests {
    /// <summary> Example of 2 entities mapped in the same index</summary>
    [Indexed(Index = "Documents")]
	public class AlternateDocument {
        private int id;
        private string title;
        private string summary;
        private string text;

        public AlternateDocument() {}

        public AlternateDocument(int id, string title, string summary, string text) {
            this.id = id;
            this.title = title;
            this.summary = summary;
            this.text = text;
        }

        [DocumentId]
        public virtual int Id {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.Tokenized, Store = Attributes.Store.Yes, Name = "alt_title")]
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