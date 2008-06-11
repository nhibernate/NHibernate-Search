using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Mark a property as indexable
    /// </summary>
    /// <remarks>We allow multiple instances of this attribute rather than having a Fields as per Java</remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class FieldAttribute : Attribute
    {
        private string name;
        private Index index = Attributes.Index.Tokenized;
        private Store store = Store.No;
        private System.Type analyzer;
        private FieldBridgeAttribute fieldBridge;

        public FieldAttribute()
        {
        }

        public FieldAttribute(Index index)
        {
            this.index = index;
        }

        /// <summary>
        /// Field name, default to the property name
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Should the value be stored in the document
        /// defaults to no.
        /// </summary>
        public Store Store
        {
            get { return store; }
            set { store = value; }
        }

        /// <summary>
        /// Defines how the Field should be indexed
        /// defaults to tokenized
        /// </summary>
        public Index Index
        {
            get { return index; }
            set { index = value; }
        }

        /// <summary>
        /// Define an analyzer for the field, default to
        /// the inherited analyzer
        /// </summary>
        public System.Type Analyzer
        {
            get { return analyzer; }
            set { analyzer = value; }
        }

        /// <summary>
        /// Field bridge used. Default is autowired.
        /// </summary>
        /// TODO: Not sure if this is correct
        public FieldBridgeAttribute FieldBridge
        {
            get { return fieldBridge; }
            set { fieldBridge = value; }
        }
    }
}