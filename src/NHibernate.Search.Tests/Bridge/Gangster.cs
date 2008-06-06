using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Bridge
{
    [Indexed]
    public class Gangster
    {
        private object id;
        private string name;

        /// <summary>
        /// 
        /// </summary>
        public virtual object Id
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