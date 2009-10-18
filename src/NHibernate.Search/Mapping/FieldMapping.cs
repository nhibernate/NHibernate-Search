using System;
using System.Collections.Generic;

using Lucene.Net.Analysis;
using NHibernate.Properties;
using NHibernate.Search.Bridge;

namespace NHibernate.Search.Mapping {
    public class FieldMapping : PropertyMappingBase
    {
        public FieldMapping(string name, IFieldBridge bridge, IGetter getter) : base(getter)
        {
            this.Name = name;
            this.Bridge = bridge;

            this.Store = Attributes.Store.No;
            this.Index = Attributes.Index.Tokenized;
        }

        public string Name              { get; private set; }
        public IFieldBridge Bridge      { get; private set; }
        public float? Boost             { get; set; }
        public Attributes.Store Store   { get; set; }
        public Attributes.Index Index   { get; set; }
        public Analyzer Analyzer        { get; set; }
    }
}
