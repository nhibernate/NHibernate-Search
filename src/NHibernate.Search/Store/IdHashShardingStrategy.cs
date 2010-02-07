using Lucene.Net.Documents;

namespace NHibernate.Search.Store
{
    using System;

    public class IdHashShardingStrategy : IIndexShardingStrategy
    {
        private IDirectoryProvider[] providers;

        #region Public methods

        public void Initialize(object properties, IDirectoryProvider[] providers)
        {
            if (providers == null)
            {
                throw new ArgumentNullException("providers");
            }

            this.providers = providers;
        }

        public IDirectoryProvider[] GetDirectoryProvidersForAllShards()
        {
            return providers;
        }

        public IDirectoryProvider GetDirectoryProviderForAddition(System.Type entity, object id, string idInString,
                                                                  Document document)
        {
            return providers[HashKey(idInString)];
        }

        public IDirectoryProvider[] GetDirectoryProvidersForDeletion(System.Type entity, object id, string idInString)
        {
            return string.IsNullOrEmpty(idInString)
                       ? providers
                       : new IDirectoryProvider[] {providers[HashKey(idInString)]};
        }

        #endregion

        #region Private methods

        private int HashKey(string key)
        {
            // Reproduce the JavaDoc version of String.hashCode so we shard the same way.
            int hash = 0;
            foreach (char c in key)
            {
                hash = (31 * hash) + c;
            }

            return hash % providers.GetLength(0);;
        }

        #endregion
    }
}