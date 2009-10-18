using System;
using System.Collections.Generic;

using NHibernate.Search.Engine;

using Lucene.Net.Analysis;

namespace NHibernate.Search.Mapping
{
    using Type = System.Type;
    
    public class DocumentMapping
    {
        public DocumentMapping(Type mappedClass) {
            this.MappedClass = mappedClass;

            this.ClassBridges = new List<ClassBridgeMapping>();
            this.Fields = new List<FieldMapping>();
            this.Embedded = new List<EmbeddedMapping>();
            this.ContainedIn = new List<ContainedInMapping>();
            this.FullTextFilterDefinitions = new List<FilterDef>();
        }

        public Type MappedClass                           { get; private set; }
        public string IndexName                           { get; set; }
        public float? Boost                               { get; set; }
        public Analyzer Analyzer                          { get; set; }

        public IList<ClassBridgeMapping> ClassBridges     { get; private set; }

        public DocumentIdMapping DocumentId               { get; set; }
        public IList<FieldMapping> Fields                 { get; private set; }
        public IList<EmbeddedMapping> Embedded            { get; private set; }

        public IList<FilterDef> FullTextFilterDefinitions { get; private set; }

        public IList<ContainedInMapping> ContainedIn      { get; private set; }
    }
}
