using System.Collections.Generic;
using Iesi.Collections.Generic;
using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Embedded
{
    [Indexed]
    public class Address
    {
        [IndexedEmbedded] 
        private Country country;

        [DocumentId] 
        private long id;

        [IndexedEmbedded(Depth = 1, Prefix = "ownedBy_", TargetElement = typeof(Owner))] 
        private Owner ownedBy;

        [Field(Index.Tokenized)] 
        private string street;

        [ContainedIn] 
        private ISet<Tower> towers = new HashSet<Tower>();

        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string Street
        {
            get { return street; }
            set { street = value; }
        }

        public virtual Owner OwnedBy
        {
            get { return ownedBy; }
            set { ownedBy = value; }
        }

        public virtual ISet<Tower> Towers
        {
            get { return towers; }
            set { towers = value; }
        }

        public virtual Country Country
        {
            get { return country; }
            set { country = value; }
        }
    }
}