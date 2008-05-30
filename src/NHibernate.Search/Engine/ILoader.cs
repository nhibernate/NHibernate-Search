using System.Collections;

namespace NHibernate.Search.Engine
{
    public interface ILoader
    {
        void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor);

        object Load(EntityInfo entityInfo);

        IList Load(params EntityInfo[] entityInfos);
    }
}