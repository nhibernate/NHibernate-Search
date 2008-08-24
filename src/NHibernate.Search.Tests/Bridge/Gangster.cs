using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Bridge
{
    [Indexed]
    public class Gangster
    {
        private int id;
        private string name;

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
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}