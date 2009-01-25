using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Reader
{
    [Indexed]
    public class Suspect
    {
        [DocumentId]
        private int id;

        [Field(Index.Tokenized)]
        private string name;

        [Field(Index.Tokenized)]
        private string physicalDescription;

        [Field(Index.Tokenized)]
        private string suspectCharge;


        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual string PhysicalDescription
        {
            get { return physicalDescription; }
            set { physicalDescription = value; }
        }

        public virtual string SuspectCharge
        {
            get { return suspectCharge; }
            set { suspectCharge = value; }
        }
    }
}