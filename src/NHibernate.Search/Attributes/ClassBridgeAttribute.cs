using System;
using System.Collections.Generic;
using NHibernate.Search.Mapping.Definition;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// This annotation allows a user to apply an implementation
    /// class to a Lucene document to manipulate it in any way
    /// the user sees fit.
    /// </summary>
    /// <remarks>
    /// We allow multiple instances of this attribute rather than having a ClassBridgesAttribute as per Java
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ClassBridgeAttribute : Attribute, IClassBridgeDefinition
    {
        private readonly System.Type impl;
        private readonly Dictionary<string, object> parameters;
        private System.Type analyzer;
        private float boost = 1.0F;
        private Index index = Index.Tokenized;
        private string name = null;
        private Store store = Store.No;

        #region Constructors

        public ClassBridgeAttribute(System.Type impl)
        {
            this.impl = impl;
            parameters = new Dictionary<string, object>();
        }

        #endregion

        #region Property methods

        /// <summary>
        /// Field name, default to the property name.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Should the value be stored in the document, defaults to no.
        /// </summary>
        public Store Store
        {
            get { return store; }
            set { store = value; }
        }

        /// <summary>
        /// Defines how the Field should be indexed defaults to tokenized.
        /// </summary>
        public Index Index
        {
            get { return index; }
            set { index = value; }
        }

        /// <summary>
        /// Define an analyzer for the field, default to the inherited analyzer.
        /// </summary>
        /// <remarks>The Java uses an Analyzer annotation here, we can't do that, so just supply the analyzer's type</remarks>
        public System.Type Analyzer
        {
            get { return analyzer; }
            set { analyzer = value; }
        }

        /// <summary>
        /// A float value of the amount of lucene defined boost to apply to a field.
        /// </summary>
        public float Boost
        {
            get { return boost; }
            set { boost = value; }
        }

        /// <summary>
        /// User supplied class to manipulate document in
        /// whatever mysterious ways they wish to.
        /// </summary>
        public System.Type Impl
        {
            get { return impl; }
        }

        /// <summary>
        /// Array of fields to work with. The imnpl class
        /// above will work on these fields.
        /// </summary>
        public Dictionary<string, object> Parameters
        {
            get { return parameters; }
        }

        #endregion
    }
}