using System;
using System.Collections.Generic;
using NHibernate.Properties;
using NHibernate.Search.Bridge;

namespace NHibernate.Search.Mapping {
    public class DocumentIdMapping : PropertyMappingBase
    {
        public DocumentIdMapping(string name, ITwoWayFieldBridge bridge, IGetter getter) : base(getter)
        {
            this.Name = name;
            this.Bridge = bridge;
        }

        public string Name               { get; private set; }
        public ITwoWayFieldBridge Bridge { get; private set; }

        public float? Boost              { get; set; }
    }
}
