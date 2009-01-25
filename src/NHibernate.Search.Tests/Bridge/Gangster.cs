using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Bridge
{
    [Indexed]
    public class Gangster
    {
        private System.Type id;
        private string name;

        /// <summary>
        /// 
        /// </summary>
        [DocumentId]
        public virtual System.Type Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}