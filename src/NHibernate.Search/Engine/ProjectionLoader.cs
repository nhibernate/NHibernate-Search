using System.Collections;
using NHibernate.Transform;

namespace NHibernate.Search.Engine
{
    public class ProjectionLoader : ILoader
    {
        private string[] aliases;
        private ObjectLoader objectLoader;
        private bool? projectThis;
        private ISearchFactoryImplementor searchFactoryImplementor;
        private ISession session;
        private IResultTransformer transformer;

        #region Private methods

        private void initThisProjectionFlag(EntityInfo entityInfo)
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

        #endregion

        #region Public methods

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

        public object Load(EntityInfo entityInfo)
        {
            initThisProjectionFlag(entityInfo);
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

            initThisProjectionFlag(entityInfos[0]);
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


        #endregion
    }
}