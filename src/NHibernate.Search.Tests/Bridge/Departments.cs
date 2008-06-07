using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Bridge
{
    [Indexed]
    [ClassBridge(typeof(CatDeptsFieldsClassBridge),
        Name = "branchnetwork",
        Index = Index.Tokenized,
        Store = Attributes.Store.Yes)]
    [Parameter("sepChar", " ", Owner = "branchnetwork")]
    [ClassBridge(typeof(EquipmentType),
        Name = "equiptype",
        Index = Index.Tokenized,
        Store = Attributes.Store.Yes)]
    [Parameter("C", "Cisco", Owner = "equiptype")]
    [Parameter("D", "D-Link", Owner = "equiptype")]
    [Parameter("K", "Kingston", Owner = "equiptype")]
    [Parameter("3", "3Com", Owner = "equiptype")]
    public class Departments
    {
        private int id;
        private string network;
        private string manufacturer;
        private string branchHead;
        private string branch;
        private int maxEmployees;

        /// <summary>
        /// 
        /// </summary>
        [DocumentId]
        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        public virtual string BranchHead
        {
            get { return branchHead; }
            set { branchHead = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        public virtual string Network
        {
            get { return network; }
            set { network = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        public virtual string Branch
        {
            get { return branch; }
            set { branch = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual int MaxEmployees
        {
            get { return maxEmployees; }
            set { maxEmployees = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual string Manufacturer
        {
            get { return manufacturer; }
            set { manufacturer = value; }
        }
    }
}