using System;

using NHibernate.Search.Attributes;
using Index = NHibernate.Search.Attributes.Index;

namespace NHibernate.Search.Tests.DirectoryProvider
{
    [Indexed]
    public class SnowStorm
    {
        private DateTime dateTime;
        private long id;
        private string location;

        /// <summary>
        /// Gets or sets Id.
        /// </summary>
        [DocumentId]
        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Gets or sets DateTime.
        /// </summary>
        [Field(Index.UnTokenized)]
        [DateBridge(Resolution.Day)]
        public virtual DateTime DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        /// <summary>
        /// Gets or sets Location.
        /// </summary>
        [Field(Index.Tokenized)]
        public virtual string Location
        {
            get { return location; }
            set { location = value; }
        }
    }
}