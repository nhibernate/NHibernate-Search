using System;
using System.Collections.Generic;

using NHibernate.Properties;

namespace NHibernate.Search.Mapping {
    public class ContainedInMapping : PropertyMappingBase {
        public ContainedInMapping(IGetter getter) : base(getter)
        {
        }
    }
}
