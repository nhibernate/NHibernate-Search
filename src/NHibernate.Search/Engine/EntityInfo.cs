using System.Collections.Generic;

namespace NHibernate.Search.Engine
{
    public class EntityInfo
    {
        private System.Type clazz;
        private object id;
        private object[] projection;
        private List<int> indexesOfThis;

        /// <summary>
        /// 
        /// </summary>
        public System.Type Clazz
        {
            get { return clazz; }
            set { clazz = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public object Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public object[] Projection
        {
            get { return projection; }
            set { projection = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<int> IndexesOfThis
        {
            get { return indexesOfThis; }
            set { indexesOfThis = value; }
        }
    }
}