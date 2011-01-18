using System;
using System.Collections;

namespace NHibernate.Search.Engine
{
    public class ObjectLoader : ILoader
    {
        private static readonly ILogger log = LoggerProvider.LoggerFor(typeof(ObjectLoader));

        private ISession session;

        #region ILoader Members

        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.session = session;
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
                if (LoaderHelper.IsObjectNotFoundException(e))
                {
                    log.Debug("Object found in Search index but not in database: "
                              + entityInfo.Clazz + " wih id " + entityInfo.Id);
                    maybeProxy = null;
                }
                else
                    throw;
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
                    if (LoaderHelper.IsObjectNotFoundException(e))
                    {
                        log.Warn("Object found in Search index but not in database: "
                                  + entityInfo.Clazz + " wih id " + entityInfo.Id);
                    }
                    else
                        throw;
                }
            }

            return result;
        }

        #endregion
    }
}