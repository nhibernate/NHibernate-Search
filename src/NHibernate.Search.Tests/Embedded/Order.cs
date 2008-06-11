using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Embedded
{
    public class Order
    {
        [DocumentId]
        private int id;

        [Field(Index.UnTokenized)]
        private string orderNumber;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string OrderNumber
        {
            get { return orderNumber; }
            set { orderNumber = value; }
        }

    }
}
