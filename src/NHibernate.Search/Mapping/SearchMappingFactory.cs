using System;
using System.Collections.Generic;

using NHibernate.Cfg;
using NHibernate.Search.Impl;
using NHibernate.Search.Mapping.AttributeBased;
using NHibernate.Util;

namespace NHibernate.Search.Mapping
{
    public static class SearchMappingFactory 
    {
        public static ISearchMapping CreateMapping(Configuration cfg)
        {
            System.Type mappingClass = GetMappingClass(cfg.GetProperty(Environment.MappingClass));

            object instance;
            try
            {
                instance = Activator.CreateInstance(mappingClass);
            }
            catch (Exception ex)
            {
                throw new SearchException(
                    "Could not create search mapping class '" + mappingClass.FullName + "'.", ex
                );
            }

            if (!(instance is ISearchMapping))
            {
                throw new SearchException(string.Format(
                    "Search mapping class '{0}' does not implement '{1}'.",
                    mappingClass.FullName, typeof(ISearchMapping).FullName
                ));
            }

            return (ISearchMapping)instance;
        }

        private static System.Type GetMappingClass(string mappingClassName) {
            if (mappingClassName == null)
                return typeof(AttributeSearchMapping);

            try
            {
                return ReflectHelper.ClassForName(mappingClassName);
            }
            catch (Exception e)
            {
                throw new SearchException(string.Format(
                    "Search mapping class '{0}' defined in property '{1}' could not be found.",
                    mappingClassName, Environment.MappingClass
                ), e);
            }
        }
    }
}
