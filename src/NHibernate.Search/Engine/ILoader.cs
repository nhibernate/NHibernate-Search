using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NHibernate.Search.Engine
{
    public interface ILoader
    {
        void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor);

        object Load(EntityInfo entityInfo);

        IList Load(params EntityInfo[] entityInfos);
    }

    public interface IAsyncLoader
    {
        void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor);

        ValueTask<object> LoadAsync(EntityInfo entityInfo, CancellationToken token);

        IAsyncEnumerable<(EntityInfo EntityInfo, Object Entity)> LoadAsync(IReadOnlyList<EntityInfo> entityInfos, CancellationToken token);
    }
}