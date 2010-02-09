using System;
using System.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Search.Backend;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Search.Mapping;
using NHibernate.Search.Store.Optimization;
using NHibernate.Util;

namespace NHibernate.Search.Store
{
    /// <summary>
    /// Creates a Lucene directory provider.
    /// <para>
    /// Lucene directory providers are configured through properties
    /// <list>
    /// <item>hibernate.search.default.* and</item>
    /// <item>hibernate.search.&lt;indexname&gt;.*</item>
    /// </list>
    /// &lt;indexname&gt; properties have precedence over default
    /// </para>
    /// <para>
    /// The implementation is described by
    /// hibernate.search.[default|indexname].directory_provider
    /// </para>
    /// If none is defined the default value is FSDirectory
    /// </summary>
    public class DirectoryProviderFactory
    {
        private const string LUCENE_DEFAULT = LUCENE_PREFIX + "default.";
        private const string LUCENE_PREFIX = "hibernate.search.";
        private const string DEFAULT_DIRECTORY_PROVIDER = "NHibernate.Search.Store.FSDirectoryProvider, NHibernate.Search";

        // Lucene index performance parameters
        private const string MERGE_FACTOR = "merge_factor";
        private const string MAX_MERGE_DOCS = "max_merge_docs";
        private const string MAX_BUFFERED_DOCS = "max_buffered_docs";
        private const string RAM_BUFFER_SIZE = "ram_buffer_size";
        private const string TERM_INDEX_INTERVAL = "term_index_interval";
        private const string BATCH = "batch.";
        private const string TRANSACTION = "transaction.";

        private const string SHARDING_STRATEGY = "sharding_strategy";
        private const string NBR_OF_SHARDS = SHARDING_STRATEGY + ".nbr_of_shards";

        private readonly List<IDirectoryProvider> providers = new List<IDirectoryProvider>();

        #region Public methods

        public DirectoryProviders CreateDirectoryProviders(DocumentMapping classMapping, Configuration cfg,
                                                          ISearchFactoryImplementor searchFactoryImplementor)
        {
            // Get properties
            string directoryProviderName = GetDirectoryProviderName(classMapping, cfg);
            IDictionary<string, string>[] indexProps = GetDirectoryProperties(cfg, directoryProviderName);

            // Set up the directories
            int nbrOfProviders = indexProps.Length;
            IDirectoryProvider[] providers = new IDirectoryProvider[nbrOfProviders];
            for (int index = 0; index < nbrOfProviders; index++)
            {
                string providerName = nbrOfProviders > 1
                                          ? directoryProviderName + "." + index
                                          : directoryProviderName;

                // NB Are the properties nested??
                providers[index] = CreateDirectoryProvider(providerName, indexProps[index], searchFactoryImplementor);
            }

            // Define sharding strategy
            IIndexShardingStrategy shardingStrategy;
            IDictionary<string, string> shardingProperties = new Dictionary<string, string>();

            // Any indexProperty will do, the indexProps[0] surely exists.
            foreach (KeyValuePair<string, string> entry in indexProps[0])
            {
                if (entry.Key.StartsWith(SHARDING_STRATEGY))
                {
                    shardingProperties.Add(entry);
                }
            }

            string shardingStrategyName;
            shardingProperties.TryGetValue(SHARDING_STRATEGY, out shardingStrategyName);
            if (string.IsNullOrEmpty(shardingStrategyName))
            {
                if (indexProps.Length == 1)
                {
                    shardingStrategy = new NotShardedStrategy();
                }
                else
                {
                    shardingStrategy = new IdHashShardingStrategy();
                }
            }
            else
            {
                try
                {
                    System.Type shardingStrategyClass = ReflectHelper.ClassForName(shardingStrategyName);
                    shardingStrategy = (IIndexShardingStrategy) Activator.CreateInstance(shardingStrategyClass);
                }
                catch
                {
                    // TODO: See if we can get a tigher exception trap here
                    throw new SearchException("Failed to instantiate lucene analyzer with type  " + shardingStrategyName);
                }
            }

            shardingStrategy.Initialize(shardingProperties, providers);

            return new DirectoryProviders(shardingStrategy, providers);
        }

        public void StartDirectoryProviders()
        {
            foreach (IDirectoryProvider provider in providers)
            {
                provider.Start();
            }
        }

        #endregion

        #region Private methods

        private static IDictionary<string, string>[] GetDirectoryProperties(Configuration cfg, string directoryProviderName)
        {
            if (string.IsNullOrEmpty(directoryProviderName))
            {
                throw new ArgumentException("Value should be not null and not empty.", "directoryProviderName");
            }

            IDictionary<string, string> props = cfg.Properties;
            string indexName = LUCENE_PREFIX + directoryProviderName;
            IDictionary<string, string> defaultProperties = new Dictionary<string, string>();
            List<IDictionary<string, string>> indexSpecificProps = new List<IDictionary<string, string>>();
            IDictionary<string, string> indexSpecificDefaultProps = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> entry in props)
            {
                string key = entry.Key;
                if (key.StartsWith(LUCENE_DEFAULT))
                {
                    defaultProperties[key.Substring(LUCENE_DEFAULT.Length)] = entry.Value;
                }
                else if (key.StartsWith(indexName))
                {
                    string suffixedKey = key.Substring(indexName.Length + 1);
                    int nextDoc = suffixedKey.IndexOf('.');
                    int index = -1;
                    if (nextDoc != -1)
                    {
                        string potentialNbr = suffixedKey.Substring(0, nextDoc);
                        if (!int.TryParse(potentialNbr, out index))
                        {
                            index = -1;
                        }
                    }

                    if (index == -1)
                    {
                        indexSpecificDefaultProps[suffixedKey] = entry.Value;
                    }
                    else
                    {
                        string finalKeyName = suffixedKey.Substring(nextDoc + 1);

                        // Ignore sharding strategy properties
                        if (!finalKeyName.StartsWith(SHARDING_STRATEGY))
                        {
                            EnsureListSize(indexSpecificProps, index + 1);
                            IDictionary<string, string> propertiesForIndex = indexSpecificProps[index];
                            if (propertiesForIndex == null)
                            {
                                propertiesForIndex = new Dictionary<string, string>();
                                indexSpecificProps[index] = propertiesForIndex;
                            }

                            propertiesForIndex[finalKeyName] = entry.Value;
                        }
                    }
                }
            }

            int nbrOfShards = -1;
            if (indexSpecificDefaultProps.ContainsKey(NBR_OF_SHARDS))
            {
                string nbrOfShardsString = indexSpecificDefaultProps[NBR_OF_SHARDS];
                if (!string.IsNullOrEmpty(nbrOfShardsString))
                {
                    if (!int.TryParse(nbrOfShardsString, out nbrOfShards))
                    {
                        throw new SearchException(indexName + "." + NBR_OF_SHARDS + " is not a number");
                    }
                }
            }

            if (nbrOfShards <= 0 && indexSpecificProps.Count == 0)
            {
                // Original java doesn't copy properties from the defaults!
                foreach (KeyValuePair<string, string> prop in defaultProperties)
                {
                    if (!indexSpecificDefaultProps.ContainsKey(prop.Key))
                    {
                        indexSpecificDefaultProps.Add(prop);
                    }
                }

                // No Shard (A sharded subindex has to have at least one property)
                return new IDictionary<string, string>[] { indexSpecificDefaultProps };
            }

            // Sharded
            nbrOfShards = nbrOfShards > indexSpecificDefaultProps.Count ? nbrOfShards : indexSpecificDefaultProps.Count;
            EnsureListSize(indexSpecificProps, nbrOfShards);

            for (int index = 0; index < nbrOfShards; index++)
            {
                if (indexSpecificProps[index] == null)
                {
                    indexSpecificProps[index] = new Dictionary<string, string>(indexSpecificDefaultProps);
                }
            }

            // Original java doesn't copy properties from the defaults!
            foreach (KeyValuePair<string, string> prop in defaultProperties)
            {
                if (!indexSpecificDefaultProps.ContainsKey(prop.Key))
                {
                    indexSpecificDefaultProps.Add(prop);
                }
            }

            foreach (IDictionary<string, string> isp in indexSpecificProps)
            {
                foreach (KeyValuePair<string, string> prop in indexSpecificDefaultProps)
                {
                    if (!isp.ContainsKey(prop.Key))
                    {
                        isp.Add(prop);
                    }
                }
            }

            return indexSpecificProps.ToArray();
        }

        private static void EnsureListSize(List<IDictionary<string, string>> indexSpecificProps, int size)
        {
            while (indexSpecificProps.Count < size)
            {
                indexSpecificProps.Add(null);
            }
        }

        private static void ConfigureOptimizerStrategy(ISearchFactoryImplementor searchFactoryImplementor, IDictionary<string, string> indexProps, IDirectoryProvider provider)
        {
            bool incremental = indexProps.ContainsKey("optimizer.operation_limit.max") ||
                               indexProps.ContainsKey("optimizer.transaction_limit.max");

            IOptimizerStrategy optimizerStrategy;
            if (incremental)
            {
                optimizerStrategy = new IncrementalOptimizerStrategy();
                optimizerStrategy.Initialize(provider, indexProps, searchFactoryImplementor);
            }
            else
            {
                optimizerStrategy = new NoOpOptimizerStrategy();
            }

            searchFactoryImplementor.AddOptimizerStrategy(provider, optimizerStrategy);
        }

        private static void ConfigureIndexingParameters(ISearchFactoryImplementor searchFactoryImplementor, IDictionary<string, string> indexProps, IDirectoryProvider provider)
        {
            LuceneIndexingParameters indexingParams = new LuceneIndexingParameters();

            ConfigureProp(
                    TRANSACTION + MERGE_FACTOR,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.MergeFactor = value;
                        indexingParams.TransactionIndexParameters.MergeFactor = value;
                    });

            ConfigureProp(
                    TRANSACTION + MAX_MERGE_DOCS,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.MaxMergeDocs = value;
                        indexingParams.TransactionIndexParameters.MaxMergeDocs = value;
                    });

            ConfigureProp(
                    TRANSACTION + MAX_BUFFERED_DOCS,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.MaxBufferedDocs = value;
                        indexingParams.TransactionIndexParameters.MaxBufferedDocs = value;
                    });

            ConfigureProp(
                    TRANSACTION + RAM_BUFFER_SIZE,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.RamBufferSizeMb = value;
                        indexingParams.TransactionIndexParameters.RamBufferSizeMb = value;
                    });

            ConfigureProp(
                    TRANSACTION + TERM_INDEX_INTERVAL,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.TermIndexInterval = value;
                        indexingParams.TransactionIndexParameters.TermIndexInterval = value;
                    });

            ConfigureProp(
                    BATCH + MERGE_FACTOR,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.MergeFactor = value;
                    });

            ConfigureProp(
                    BATCH + MAX_MERGE_DOCS,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.MaxMergeDocs = value;
                    });

            ConfigureProp(
                    BATCH + MAX_BUFFERED_DOCS,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.MaxBufferedDocs = value;
                    });

            ConfigureProp(
                    BATCH + RAM_BUFFER_SIZE,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.RamBufferSizeMb = value;
                    });

            ConfigureProp(
                    BATCH + TERM_INDEX_INTERVAL,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchIndexParameters.TermIndexInterval = value;
                    });

            searchFactoryImplementor.AddIndexingParameters(provider, indexingParams);
        }

        private static void ConfigureProp(string name, IDictionary<string, string> indexProps, Action<int> assign)
        {
            string prop;
            if (!indexProps.TryGetValue(name, out prop) || string.IsNullOrEmpty(prop))
            {
                return;
            }

            int value;
            if (int.TryParse(prop, out value))
            {
                assign(value);
            }
            else
            {
                throw new SearchException("Invalid value for " + name + ": " + prop);
            }
        }

        private IDirectoryProvider CreateDirectoryProvider(string directoryProviderName, IDictionary<string, string> indexProps, ISearchFactoryImplementor searchFactoryImplementor)
        {
            string className;
            indexProps.TryGetValue("directory_provider", out className);
            if (StringHelper.IsEmpty(className))
            {
                className = DEFAULT_DIRECTORY_PROVIDER;
            }

            IDirectoryProvider provider;
            try
            {
                System.Type directoryClass = ReflectHelper.ClassForName(className);
                provider = (IDirectoryProvider)Activator.CreateInstance(directoryClass);
            }
            catch (Exception e)
            {
                throw new HibernateException("Unable to instantiate directory provider: " + className, e);
            }

            try
            {
                provider.Initialize(directoryProviderName, indexProps, searchFactoryImplementor);
            }
            catch (Exception e)
            {
                throw new HibernateException("Unable to initialize: " + directoryProviderName, e);
            }

            int index = providers.IndexOf(provider);
            if (index != -1)
            {
                // Share the same Directory provider for the same underlying store
                return providers[index];
            }

            ConfigureOptimizerStrategy(searchFactoryImplementor, indexProps, provider);
            ConfigureIndexingParameters(searchFactoryImplementor, indexProps, provider);
            providers.Add(provider);
            if (!searchFactoryImplementor.GetLockableDirectoryProviders().ContainsKey(provider))
            {
                searchFactoryImplementor.GetLockableDirectoryProviders()[provider] = new object();
            }

            return provider;
        }

        private string GetDirectoryProviderName(DocumentMapping documentMapping, Configuration cfg)
        {
            return string.IsNullOrEmpty(documentMapping.IndexName)
                 ? documentMapping.MappedClass.Name
                 : documentMapping.IndexName;
        }

        #endregion

        #region Nested class : DirectoryProviders

        public class DirectoryProviders
        {
            private readonly IIndexShardingStrategy shardingStrategy;
            private readonly IDirectoryProvider[] providers;

            public DirectoryProviders(IIndexShardingStrategy shardingStrategy, IDirectoryProvider[] providers)
            {
                this.shardingStrategy = shardingStrategy;
                this.providers = providers;
            }

            public IIndexShardingStrategy SelectionStrategy
            {
                get { return shardingStrategy; }
            }

            public IDirectoryProvider[] Providers
            {
                get { return providers; }
            }
        }

        #endregion
    }
}