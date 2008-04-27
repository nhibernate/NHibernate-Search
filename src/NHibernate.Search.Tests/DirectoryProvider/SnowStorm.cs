using System;
using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.DirectoryProvider {
    [Indexed]
    public class SnowStorm {
        private DateTime dateTime;
        private long id;
        private string location;

        [DocumentId]
        public virtual long Id {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.UnTokenized)]
        [DateBridge(Resolution.Day)]
        public virtual DateTime DateTime {
            get { return dateTime; }
            set { dateTime = value; }
        }

        [Field(Index.Tokenized)]
        public virtual string Location {
            get { return location; }
            set { location = value; }
        }
    }
}