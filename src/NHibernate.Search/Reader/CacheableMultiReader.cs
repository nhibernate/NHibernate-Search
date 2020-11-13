using Lucene.Net.Index;
using Lucene.Net.Search;

namespace NHibernate.Search.Reader
{
    /// <summary>
    /// MultiReader ensuring equals returns true if the underlying readers are the same (and in the same order)
    /// Especially useful when using <see cref="CachingWrapperFilter" />
    /// </summary>
    public class CacheableMultiReader : MultiReader
    {
        private new readonly IndexReader[] subReaders;

        public CacheableMultiReader(IndexReader[] subReaders) : base(subReaders)
        {
            this.subReaders = subReaders;
        }

        private bool BusinessEquals(CacheableMultiReader other)
        {
            if (other == null) return false;

            int length = subReaders.Length;
            if (length != other.subReaders.Length) return false;
            for (int index = 0; index < length; index++)
                if (!subReaders[index].Equals(other.subReaders[index]))
                    return false;

            return true;
        }


        public bool Equals(CacheableMultiReader obj)
        {
            // NB We need to cast down to obj to get the simple "=="
            if (obj == null) return false;

            return BusinessEquals(obj);
        }
  }
}