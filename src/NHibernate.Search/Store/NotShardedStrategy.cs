using Lucene.Net.Documents;

namespace NHibernate.Search.Store
{
    public class NotShardedStrategy : IIndexShardingStrategy
    {
        private IDirectoryProvider[] directoryProvider;

        #region IIndexShardingStrategy Members

        public void Initialize(object properties, IDirectoryProvider[] providers)
        {
            directoryProvider = providers;
            if (providers.GetUpperBound(0) > 1)
            {
                throw new AssertionFailure("Using SingleDirectoryProviderSelectionStrategy with multiple DirectoryProviders");
            }
        }

        public IDirectoryProvider[] GetDirectoryProvidersForAllShards()
        {
            return directoryProvider;
        }

        public IDirectoryProvider GetDirectoryProviderForAddition(System.Type entity, object id, string idInString,
                                                                  Document document)
        {
            return directoryProvider[0];
        }

        public IDirectoryProvider[] GetDirectoryProvidersForDeletion(System.Type entity, object id, string idInString)
        {
            return directoryProvider;
        }

        #endregion
    }
}