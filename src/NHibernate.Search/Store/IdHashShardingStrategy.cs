using Lucene.Net.Documents;

namespace NHibernate.Search.Store
{
    public class IdHashShardingStrategy : IIndexShardingStrategy
    {
        private IDirectoryProvider[] providers;

        #region Private methods

        private int hashKey(string key)
        {
            int divisor = providers.GetUpperBound(0) != 0 ? providers.GetUpperBound(0) : 1;
            // http://bmaurer.blogspot.com/2006/10/mathabs-returns-negative-number.html
            // Strings are invariant in .NET so just do the division rather than compute the value
            return (key.GetHashCode() & 0x7fffffff) % divisor;
        }

        #endregion

        #region IIndexShardingStrategy Members

        public void Initialize(object properties, IDirectoryProvider[] providers)
        {
            this.providers = providers;
        }

        public IDirectoryProvider[] GetDirectoryProvidersForAllShards()
        {
            return providers;
        }

        public IDirectoryProvider GetDirectoryProviderForAddition(System.Type entity, object id, string idInString,
                                                                  Document document)
        {
            return providers[hashKey(idInString)];
        }

        public IDirectoryProvider[] GetDirectoryProvidersForDeletion(System.Type entity, object id, string idInString)
        {
            return string.IsNullOrEmpty(idInString)
                       ? providers
                       : new IDirectoryProvider[] {providers[hashKey(idInString)]};
        }

        #endregion
    }
}