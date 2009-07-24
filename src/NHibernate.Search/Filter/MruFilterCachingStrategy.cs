using System;
using System.Collections.Generic;
using NHibernate.Util;

namespace NHibernate.Search.Filter
{
    public class MruFilterCachingStrategy : IFilterCachingStrategy 
    {
        private const string SIZE = Environment.FilterCachingStrategy + ".size";
        private SoftLimitMRUCache cache;

        #region IFilterCachingStrategy Members

        public void Initialize(IDictionary<string, string> properties)
        {
            int size = 128;
            if (properties.ContainsKey(SIZE))
            {
                if (!int.TryParse(properties[SIZE], out size))
                {
                    // TODO: Log a warning
                    size = 128;
                }
            }

            cache = new SoftLimitMRUCache(size);
        }

        public Lucene.Net.Search.Filter GetCachedFilter(FilterKey key)
        {
            try
            {
                return (Lucene.Net.Search.Filter) cache[key];
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        public void AddCachedFilter(FilterKey key, Lucene.Net.Search.Filter filter)
        {
            cache.Put(key, filter);
        }

        #endregion
    }
}