using System.Collections.Generic;
using Iesi.Collections.Generic;
using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Query
{
    [Indexed]
    public class Music
    {
        private long id;
        private string title;
        private ISet<Author> authors = new HashSet<Author>();

        [DocumentId]
        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        [IndexedEmbedded(Depth=1)]
        public virtual ISet<Author> Authors
        {
            get { return authors; }
        }

        [Field(Name="title", Index=Index.Tokenized)]
        public virtual string Title
        {
            get { return title; }
            set { title = value; }
        }
    }
}