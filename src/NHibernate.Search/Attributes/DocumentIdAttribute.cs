using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Declare a field as the document id. If set to a property, the property will be used
    /// Note that <see cref="FieldBridgeAttribute" /> must return the Entity id
    /// </summary>
    /// TODO: If set to a class, the class itself will be passed to the FieldBridge
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DocumentIdAttribute : Attribute
    {
        private string name = null;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}