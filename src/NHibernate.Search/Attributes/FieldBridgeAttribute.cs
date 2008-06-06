using System;
using System.Collections.Generic;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Specifies a given field bridge implementation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FieldBridgeAttribute : Attribute
    {
        private readonly System.Type impl;
        private readonly Dictionary<string, object> parameters;

        public FieldBridgeAttribute(System.Type impl)
        {
            this.impl = impl;
            parameters = new Dictionary<string, object>();
        }

        public System.Type Impl
        {
            get { return impl; }
        }

        public Dictionary<string, object> Parameters
        {
            get { return parameters; }
        }
    }
}