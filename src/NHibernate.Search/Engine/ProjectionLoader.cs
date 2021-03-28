using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Transform;

namespace NHibernate.Search.Engine
{
    public class ProjectionLoader : ILoader, IAsyncLoader
    {
        private string[] aliases;
        private ObjectLoader objectLoader;
        private bool? projectThis;
        private ISearchFactoryImplementor searchFactoryImplementor;
        private ISession session;
        private IResultTransformer transformer;

        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.session = session;
            this.searchFactoryImplementor = searchFactoryImplementor;
        }

        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor,
            IResultTransformer transformer, string[] aliases)
        {
            Init(session, searchFactoryImplementor);
            this.transformer = transformer;
            this.aliases = aliases;
        }

        /// <inheritdoc />
        public async ValueTask<Object> LoadAsync(EntityInfo entityInfo, CancellationToken token = default)
        {
            InitThisProjectionFlag(entityInfo);
            if (projectThis == true)
            {
                foreach (int index in entityInfo.IndexesOfThis)
                {
                    entityInfo.Projection[index] = await objectLoader.LoadAsync(entityInfo, token);
                }
            }

            return transformer != null
                ? transformer.TransformTuple(entityInfo.Projection, aliases)
                : entityInfo.Projection;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<(EntityInfo EntityInfo, Object Entity)> LoadAsync(IReadOnlyList<EntityInfo> entityInfos, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (entityInfos.Count == 0) yield break;

            InitThisProjectionFlag(entityInfos[0]);
            if (projectThis == true)
            {
                var entities = objectLoader.LoadAsync(entityInfos, token); // Load by batch
                await foreach ((EntityInfo info, object entity) in entities.WithCancellation(token))
                {
                    foreach (int index in info.IndexesOfThis)
                    {
                        //set one by one to avoid loosing null objects (skipped in the objectLoader.load( EntityInfo[] ))
                        info.Projection[index] = entity;
                    }

                    if (transformer != null)
                        yield return (info, transformer.TransformTuple(info.Projection, aliases));
                    else
                        yield return (info, info.Projection);
                }
            }
        }

        public object Load(EntityInfo entityInfo)
        {
            InitThisProjectionFlag(entityInfo);
            if (projectThis == true)
            {
                foreach (int index in entityInfo.IndexesOfThis)
                {
                    entityInfo.Projection[index] = objectLoader.Load(entityInfo);
                }
            }

            return transformer != null
                       ? transformer.TransformTuple(entityInfo.Projection, aliases)
                       : entityInfo.Projection;
        }

        public IList Load(params EntityInfo[] entityInfos)
        {
            IList results = new ArrayList(entityInfos.Length);
            if (entityInfos.Length == 0) return results;

            InitThisProjectionFlag(entityInfos[0]);
            if (projectThis == true)
            {
                objectLoader.Load(entityInfos); // Load by batch
                foreach (EntityInfo entityInfo in entityInfos)
                {
                    foreach (int index in entityInfo.IndexesOfThis)
                    {
                        //set one by one to avoid loosing null objects (skipped in the objectLoader.load( EntityInfo[] ))
                        entityInfo.Projection[index] = objectLoader.Load(entityInfo);
                    }
                }
            }
            foreach (EntityInfo entityInfo in entityInfos)
            {
                if (transformer != null)
                    results.Add(transformer.TransformTuple(entityInfo.Projection, aliases));
                else
                    results.Add(entityInfo.Projection);
            }

            return results;
        }

        private void InitThisProjectionFlag(EntityInfo entityInfo)
        {
            if (projectThis == null)
            {
                projectThis = entityInfo.IndexesOfThis != null;
                if (projectThis == true)
                {
                    //TODO use QueryLoader when possible
                    objectLoader = new ObjectLoader();
                    objectLoader.Init(session, searchFactoryImplementor);
                }
            }
        }
    }
}