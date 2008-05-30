using System;
using System.Collections;

namespace NHibernate.Search.Engine
{
    public class QueryLoader : ILoader
    {
        #region ILoader Members

        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object Load(EntityInfo entityInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IList Load(params EntityInfo[] entityInfos)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}