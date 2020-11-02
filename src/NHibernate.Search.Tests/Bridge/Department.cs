using System;
using System.Collections.Generic;
using System.Text;
using NHibernate.Search.Attributes;
using Index = NHibernate.Search.Attributes.Index;

namespace NHibernate.Search.Tests.Bridge
{
    [Indexed]
    [ClassBridge(typeof(CatFieldsClassBridge),
                Name="branchnetwork", 
                Index=Index.Tokenized, 
                Store=Attributes.Store.Yes)]
    [Parameter("sepChar", " ")]
    public class Department
    {
        private int id;
        private string network;
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
        [Field(Index.Tokenized, Store=Attributes.Store.Yes)]
        public virtual string BranchHead
        {
            get { return branchHead; }
            set { branchHead = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Field(Index.Tokenized, Store=Attributes.Store.Yes)]
        public virtual string Network
        {
            get { return network; }
            set { network = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Field(Index.Tokenized, Store=Attributes.Store.Yes)]
        public virtual string Branch
        {
            get { return branch; }
            set { branch = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Field(Index.UnTokenized, Store=Attributes.Store.Yes)]
        public virtual int MaxEmployees
        {
            get { return maxEmployees; }
            set { maxEmployees = value; }
        }
    }
}
