using System;
using System.Collections.Generic;

using NHibernate.Cfg;

using NHibernate.Search.Attributes;

namespace NHibernate.Search.Mapping.AttributeBased
{
    using Type = System.Type;

    public class AttributeSearchMapping : ISearchMapping
    {
        public ICollection<DocumentMapping> Build(Configuration cfg)
        {
            var indexedClasses = new List<DocumentMapping>();
            var builder = new AttributeSearchMappingBuilder();

            foreach (var mapping in cfg.ClassMappings)
            {
                if (!this.IsIndexed(mapping.MappedClass))
                    continue;

                indexedClasses.Add(builder.Build(mapping.MappedClass));
            }

            return indexedClasses;
        }

        private bool IsIndexed(Type type)
        {
            return AttributeUtil.HasAttribute<IndexedAttribute>(type);
        }
    }
}
