using System;
using System.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Mapping;
using NHibernate.Search.Attributes;
using NHibernate.Search.Backend;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
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
        public List<IDirectoryProvider> providers = new List<IDirectoryProvider>();

        private const string LUCENE_DEFAULT = LUCENE_PREFIX + "default.";
        private const string LUCENE_PREFIX = "hibernate.search.";
        private const string DEFAULT_DIRECTORY_PROVIDER = "NHibernate.Search.Store.FSDirectoryProvider, NHibernate.Search";

        // Lucene index performance parameters
        private const string MERGE_FACTOR = "merge_factor";
        private const string MAX_MERGE_DOCS = "max_merge_docs";
        private const string MAX_BUFFERED_DOCS = "max_buffered_docs";
        private const string BATCH = "batch.";
        private const string TRANSACTION = "transaction.";

        private const string SHARDING_STRATEGY = "sharding_strategy";
        private const string NBR_OF_SHARDS = SHARDING_STRATEGY + ".nbr_of_shards";

        #region Public methods

        public DirectoryProviders CreateDirectoryProviders(System.Type entity, Configuration cfg,
                                                          ISearchFactoryImplementor searchFactoryImplementor)
        {
            // Get properties
            String directoryProviderName = GetDirectoryProviderName(entity, cfg);
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

        private void ConfigureOptimizerStrategy(ISearchFactoryImplementor searchFactoryImplementor, IDictionary<string, string> indexProps, IDirectoryProvider provider)
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

        private void ConfigureIndexingParameters(ISearchFactoryImplementor searchFactoryImplementor, IDictionary<string, string> indexProps, IDirectoryProvider provider)
        {
            LuceneIndexingParameters indexingParams = new LuceneIndexingParameters();

            ConfigureProp(
                    TRANSACTION + MERGE_FACTOR,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchMergeFactor = value;
                        indexingParams.TransactionMergeFactor = value;
                    });

            ConfigureProp(
                    TRANSACTION + MAX_MERGE_DOCS,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchMaxMergeDocs = value;
                        indexingParams.TransactionMaxMergeDocs = value;
                    });

            ConfigureProp(
                    TRANSACTION + MAX_BUFFERED_DOCS,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchMaxBufferedDocs = value;
                        indexingParams.TransactionMaxBufferedDocs = value;
                    });
            ConfigureProp(
                    BATCH + MERGE_FACTOR,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchMergeFactor = value;
                    });

            ConfigureProp(
                    BATCH + MAX_MERGE_DOCS,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchMaxMergeDocs = value;
                    });

            ConfigureProp(
                    BATCH + MAX_BUFFERED_DOCS,
                    indexProps,
                    delegate(int value)
                    {
                        indexingParams.BatchMaxBufferedDocs = value;
                    });

            searchFactoryImplementor.AddIndexingParameters(provider, indexingParams);
        }

        private void ConfigureProp(string name, IDictionary<string, string> indexProps, Action<int> assign)
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

        private IDirectoryProvider CreateDirectoryProvider(string directoryProviderName, IDictionary<string, string> indexProps,
                                                          ISearchFactoryImplementor searchFactoryImplementor)
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

        private static IDictionary<string, string>[] GetDirectoryProperties(Configuration cfg, String directoryProviderName)
        {
            string shardsCountValue;
            bool hasShards = cfg.Properties.TryGetValue(NBR_OF_SHARDS, out shardsCountValue);
            if (!hasShards || string.IsNullOrEmpty(shardsCountValue))
            {
                return new IDictionary<string, string>[] {GetIndexProps(directoryProviderName, cfg)};
            }

            int shardsCount = Int32.Parse(shardsCountValue);
            IDictionary<string, string>[] shardLocalProperties = new IDictionary<string, string>[shardsCount];
            for (int i = 0; i < shardsCount; i++)
            {
                shardLocalProperties[i] = CreateMaskedIndexProps(i.ToString(), directoryProviderName, cfg);
            }

            return shardLocalProperties;
        }

        private static void EnsureListSize(List<IDictionary<string, string>> indexSpecificProps, int size)
        {
            while (indexSpecificProps.Count < size)
            {
                indexSpecificProps.Add(null);
            }
        }

        private static IDictionary<string, string> GetIndexProps(string directoryProviderName, Configuration cfg)
        {
            IDictionary<string, string> indexProps = new Dictionary<string, string>();
            String indexName = LUCENE_PREFIX + directoryProviderName;
            IDictionary<string, string> indexSpecificProps = new Dictionary<string, string>();
            IDictionary<string, string> props = cfg.Properties;
            foreach (KeyValuePair<string, string> entry in props)
            {
                string key = entry.Key;
                if (key.StartsWith(LUCENE_DEFAULT))
                {
                    indexProps[key.Substring(LUCENE_DEFAULT.Length)] = entry.Value;
                }
                else if (key.StartsWith(indexName))
                {
                    indexSpecificProps[key.Substring(indexName.Length)] = entry.Value;
                }
            }

            foreach (KeyValuePair<string, string> indexSpecificProp in indexSpecificProps)
            {
                indexProps[indexSpecificProp.Key] = indexSpecificProp.Value;
            }

            return indexProps;
        }

        private static IDictionary<string, string> CreateMaskedIndexProps(string mask, string directoryProviderName, Configuration cfg)
        {
            /// this is a seudo implementation, it does not really take into account different NbrOfShard
            // todo: fix this
            return GetIndexProps(directoryProviderName, cfg);
        }

        private static string GetDirectoryProviderName(System.Type clazz, Configuration cfg)
        {
            // Get the most specialized (ie subclass > superclass) non default index name
            // If none extract the name from the most generic (superclass > subclass) [Indexed] class in the hierarchy
            PersistentClass pc = cfg.GetClassMapping(clazz);
            System.Type rootIndex = null;
            do
            {
                IndexedAttribute indexAnn = AttributeUtil.GetIndexed(pc.MappedClass);
                if (indexAnn != null)
                {
                    if (string.IsNullOrEmpty(indexAnn.Index) == false)
                    {
                        return indexAnn.Index;
                    }

                    rootIndex = pc.MappedClass;
                }

                pc = pc.Superclass;
            } while (pc != null);

            // there is nobody out there with a non default [Indexed(Index = "fo")]
            if (rootIndex != null)
            {
                return rootIndex.Name;
            }

            throw new HibernateException("Trying to extract the index name from a non @Indexed class: " + clazz);
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