using Iesi.Collections.Generic;
using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Embedded
{
    public class Address
    {
        [IndexedEmbedded] 
        private Country country;

        [DocumentId] 
        private long id;

        [IndexedEmbedded(Depth = 1, Prefix = "ownedBy_", TargetElement = typeof(Owner))] 
        private Person ownedBy;

        [Field(Index.Tokenized)] 
        private string street;

        [ContainedIn] 
        private ISet<Tower> towers = new HashedSet<Tower>();

        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Street
        {
            get { return street; }
            set { street = value; }
        }

        public Person OwnedBy
        {
            get { return ownedBy; }
            set { ownedBy = value; }
        }

        public ISet<Tower> Towers
        {
            get { return towers; }
            set { towers = value; }
        }

        public Country Country
        {
            get { return country; }
            set { country = value; }
        }
    }
}