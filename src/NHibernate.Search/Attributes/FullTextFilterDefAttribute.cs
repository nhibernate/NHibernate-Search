using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Defines a FullTextFilter that can be optionally applied to every FullText Queries
    /// While not related to a specific indexed entity, the annotation has to be set on one of them
    /// </summary>
    /// <remarks>We allow multiple instances of this attribute rather than having a FullTextFilterDefsAttribute as per Java</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class FullTextFilterDefAttribute : Attribute
    {
        private readonly string name;
        private readonly System.Type impl;
        private bool cache = true;

        public FullTextFilterDefAttribute(string name, System.Type impl)
        {
            this.name = name;
            this.impl = impl;
        }

        /// <summary>
        /// Filter name. Must be unique accross all mappings for a given persistence unit
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Either implements <see cref="Lucene.Net.Search.Filter" />
        /// or contains a <see cref="FactoryAttribute" /> method returning one.
        /// The Filter generated must be thread-safe
        ///
        /// If the filter accept parameters, an <see cref="KeyAttribute" /> method must be present as well.
        /// </summary>
        public System.Type Impl
        {
            get { return impl; }
        }

        /// <summary>
        /// Enable caching for this filter (default true).
        /// </summary>
        public bool Cache
        {
            get { return cache; }
            set { cache = value; }
        }
    }
}