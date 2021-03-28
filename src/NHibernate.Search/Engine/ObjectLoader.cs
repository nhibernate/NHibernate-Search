using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NHibernate.Search.Engine
{
    public class ObjectLoader : ILoader, IAsyncLoader
    {
        private static readonly INHibernateLogger log = NHibernateLogger.For(typeof(ObjectLoader));

        private ISession session;

        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <inheritdoc />
        public async ValueTask<Object> LoadAsync(EntityInfo entityInfo, CancellationToken token = default)
        {
            object maybeProxy = await session.GetAsync(entityInfo.Clazz, entityInfo.Id, token);
            // TODO: Initialize call and error trapping
            try
            {
                await NHibernateUtil.InitializeAsync(maybeProxy, token);
            }
            catch (Exception e)
            {
                if (!IsEntityNotFound(e, entityInfo))
                {
                    throw;
                }

                maybeProxy = null;
            }

            return maybeProxy;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<(EntityInfo EntityInfo, Object Entity)> LoadAsync(IReadOnlyList<EntityInfo> entityInfos, [EnumeratorCancellation] CancellationToken token = default)
        {
            // Use load to benefit from the batch-size
            // We don't face proxy casting issues since the exact class is extracted from the index
            // TODO: Why do this?
            //foreach (EntityInfo entityInfo in entityInfos)
            //{
            //    await session.LoadAsync(entityInfo.Clazz, entityInfo.Id);
            //}

            foreach (EntityInfo entityInfo in entityInfos)
            {
                object entity;

                try
                {
                    entity = await session.LoadAsync(entityInfo.Clazz, entityInfo.Id, token);
                    await NHibernateUtil.InitializeAsync(entity, token);
                }
                catch (Exception e)
                {
                    if (!IsEntityNotFound(e, entityInfo))
                    {
                        throw;
                    }

                    entity = null;
                }

                yield return (entityInfo, entity);
            }
        }

        public object Load(EntityInfo entityInfo)
        {
            object maybeProxy = session.Get(entityInfo.Clazz, entityInfo.Id);
            // TODO: Initialize call and error trapping
            try
            {
                NHibernateUtil.Initialize(maybeProxy);
            }
            catch (Exception e)
            {
                if (!IsEntityNotFound(e, entityInfo))
                {
                    throw;
                }

                maybeProxy = null;
            }

            return maybeProxy;
        }

        public IList Load(params EntityInfo[] entityInfos)
        {
            // Use load to benefit from the batch-size
            // We don't face proxy casting issues since the exact class is extracted from the index
            foreach (EntityInfo entityInfo in entityInfos) // TODO: Why do this?
            {
                session.Load(entityInfo.Clazz, entityInfo.Id);
            }

            ArrayList result = new ArrayList(entityInfos.Length);

            foreach (EntityInfo entityInfo in entityInfos)
            {
                try
                {
                    object entity = session.Load(entityInfo.Clazz, entityInfo.Id);
                    NHibernateUtil.Initialize(entity);
                    result.Add(entity);
                }
                catch (Exception e)
                {
                    if (!IsEntityNotFound(e, entityInfo))
                    {
                        throw;
                    }
                }
            }

            return result;
        }

        private static Boolean IsEntityNotFound(Exception e, EntityInfo entityInfo)
        {
            if (LoaderHelper.IsObjectNotFoundException(e))
            {
                log.Warn("Object found in Search index but not in database: {0} with id {1}", entityInfo.Clazz, entityInfo.Id);
                return true;
            }

            return false;
        }
    }
}