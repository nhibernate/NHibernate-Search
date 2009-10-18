using System;
using System.Reflection;
using NHibernate.Properties;

namespace NHibernate.Search.Backend
{
    /// <summary>
    /// work unit. Only make sense inside the same session since it uses the scope principle
    /// </summary>
    public class Work
    {
        private readonly object entity;
        private readonly object id;
        private readonly IGetter idGetter;
        private readonly WorkType workType;

        public Work(object entity, Object id, WorkType type)
        {
            this.entity = entity;
            this.id = id;
            workType = type;
        }

        public Work(object entity, IGetter idGetter, WorkType type)
        {
            this.entity = entity;
            this.idGetter = idGetter;
            workType = type;
        }

        public object Entity
        {
            get { return entity; }
        }

        public object Id
        {
            get { return id; }
        }

        public IGetter IdGetter
        {
            get { return idGetter; }
        }

        public WorkType WorkType
        {
            get { return workType; }
        }
    }
}