using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Specifies that an association (ToOne or Embedded) is to be indexed in the root entity index
    /// </summary>
    /// <remarks>
    /// It allows queries involving associated objects restrictions
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public class IndexedEmbeddedAttribute : Attribute
    {
        private string prefix = ".";
        private int depth = int.MaxValue;
        private System.Type targetElement;

        /// <summary>
        /// Field name prefix
        /// Default to 'propertyname.'
        /// </summary>
        public string Prefix
        {
            get { return prefix; }
            set { prefix = value; }
        }

        /// <summary>
        // Stop indexing embedded elements when depth is reached
        // depth=1 means the associated element is index, but not its embedded elements
        // Default: infinite (an exception will be raised in case of class circular reference when infinite is chosen)
        /// </summary>
        public int Depth
        {
            get { return depth; }
            set { depth = value; }
        }

        /// <summary>
        /// Overrides the type of an association. If a collection, overrides the type of the collection generics
        /// </summary>
        public System.Type TargetElement
        {
            get { return targetElement; }
            set { targetElement = value; }
        }
    }
}