using System;
using System.Collections.Generic;
using NHibernate.Mapping;
using NHibernate.Properties;
using NHibernate.Search.Bridge;

namespace NHibernate.Search.Mapping {
    public class DocumentIdMapping : PropertyMappingBase
    {
        public const string DefaultIndexedName = "id";

        public DocumentIdMapping(ITwoWayFieldBridge bridge, IGetter getter)
            : this(DefaultIndexedName, RootClass.DefaultIdentifierColumnName, bridge, getter)
        {
        }

        public DocumentIdMapping(string name, ITwoWayFieldBridge bridge, IGetter getter) 
            : this(name, RootClass.DefaultIdentifierColumnName, bridge, getter)
        {
        }

        public DocumentIdMapping(string name, string propertyName, ITwoWayFieldBridge bridge, IGetter getter) : base(getter)
        {
            this.Name = name;
            this.PropertyName = propertyName;
            this.Bridge = bridge;
        }

        public string Name               { get; private set; }
        public string PropertyName       { get; private set; }
        public ITwoWayFieldBridge Bridge { get; private set; }

        public float? Boost              { get; set; }
    }
}
