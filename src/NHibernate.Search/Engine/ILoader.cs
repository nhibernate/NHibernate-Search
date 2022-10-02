using System.Collections;

namespace NHibernate.Search.Engine
{
    public partial interface ILoader
    {
        void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor);

        object Load(EntityInfo entityInfo);

        IList Load(EntityInfo[] entityInfos);
    }
}