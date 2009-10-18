using System;
using System.Collections.Generic;
using NHibernate.Properties;

namespace NHibernate.Search.Mapping
{
    public class EmbeddedMapping : PropertyMappingBase
    {
        public EmbeddedMapping(DocumentMapping @class, IGetter getter) : base(getter)
        {
            this.Class = @class;
            this.Prefix = string.Empty;

            this.IsCollection = true;
        }

        public DocumentMapping Class { get; private set; }
        public string Prefix         { get; set; }
        public bool IsCollection     { get; set; }
    }
}
