using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Specifies a given field bridge implementation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FieldBridgeAttribute : Attribute
    {
        private readonly System.Type impl;
        private readonly object[] parameters;

        public FieldBridgeAttribute(System.Type impl, params object[] parameters)
        {
            this.impl = impl;
            this.parameters = parameters;
        }

        public System.Type Impl
        {
            get { return impl; }
        }

        public object[] Parameters
        {
            get { return parameters; }
        }
    }
}