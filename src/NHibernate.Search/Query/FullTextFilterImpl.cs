namespace NHibernate.Search.Query
{
    using System.Collections.Generic;

    public class FullTextFilterImpl : IFullTextFilter
    {
        private readonly Dictionary<string, object> parameters = new Dictionary<string, object>();
        private string name;

        /// <summary>
        /// Gets or sets the name of the filter.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Gets the collection of parameters
        /// </summary>
        public Dictionary<string, object> Parameters
        {
            get { return parameters; }
        }

        #region IFullTextFilter Members

        public IFullTextFilter SetParameter(string name, object value)
        {
            parameters[name] = value;

            return this;
        }

        public object GetParameter(string name)
        {
            return parameters[name];
        }

        #endregion
    }
}