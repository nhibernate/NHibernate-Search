using System;

namespace NHibernate.Search.Query
{
    public class FullTextFilterImpl : IFullTextFilter
    {
        #region IFullTextFilter Members

        public IFullTextFilter SetParameter(string name, object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object GetParameter(string name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}