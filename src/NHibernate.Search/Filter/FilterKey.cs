namespace NHibernate.Search.Filter
{
    /// <summary>
    /// The key object must implement equals / hashcode so that 2 keys are equals if and only if
    /// the given Filter types are the same and the set of parameters are the same.
    /// <para>
    /// The FilterKey creator (ie the @Key method) does not have to inject <code>impl</code>
    /// It will be done by Hibernate Search
    /// </para>
    /// </summary>
    public abstract class FilterKey
    {
        private System.Type impl;

        /// <summary>
        /// Represent the @FullTextFilterDef.impl class
        /// </summary>
        public virtual System.Type Impl
        {
            get { return impl; }
            set { impl = value; }
        }
    }
}