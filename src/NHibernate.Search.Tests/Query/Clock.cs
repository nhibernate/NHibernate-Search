namespace NHibernate.Search.Tests.Query
{
    using Attributes;

    [Indexed]
    public class Clock
    {
        private string brand;

        private int id;

        public Clock()
        {
        }

        public Clock(int id, string brand)
        {
            this.id = id;
            this.brand = brand;
        }

        [DocumentId]
        public virtual int Id
        {
            get { return this.id; }
            set { this.id = value; }
        }

        [Field(Index.Tokenized, Store = Store.Yes)]
        public virtual string Brand
        {
            get { return this.brand; }
            set { this.brand = value; }
        }
    }
}