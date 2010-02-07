namespace NHibernate.Search.Tests.Embedded.DoubleInsert
{
    using Attributes;

    [Indexed]
    public class BusinessContact : Contact
    {
        [Field(Index = Index.Tokenized, Store = Store.Yes)] 
        private string businessName;

        [Field(Index = Index.Tokenized, Store = Store.Yes)] 
        private string url;

        public string BusinessName
        {
            get { return this.businessName; }
            set { this.businessName = value; }
        }

        public string Url
        {
            get { return string.IsNullOrEmpty(this.url) ? "Not provided" : this.url; }
            set { this.url = value; }
        }
    }
}