using System;
using Lucene.Net.Index;
using NHibernate.Search.Impl;

namespace NHibernate.Search.Reader
{
    /// <summary>
    /// 
    /// </summary>
    public static class ReaderProviderHelper
    {
        public static IndexReader BuildMultiReader(int length, IndexReader[] readers)
        {
            if (length == 0)
                return null;

            try
            {
                // NB Everything should be the same so wrap in a CacheableMultiReader even if there's only one.
                return new CacheableMultiReader(readers);
            }
            catch (Exception e)
            {
                Clean(readers);
                throw new SearchException("Unable to open a MultiReader", e);
            }
        }

        public static void Clean(params IndexReader[] readers)
        {
            foreach (IndexReader reader in readers)
            {
                try
                {
                    reader.Dispose();
                }
                catch (Exception)
                {
                    // Swallow
                }
            }
        }
    }
}