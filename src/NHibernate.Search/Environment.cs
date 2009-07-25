using NHibernate.Search.Filter;

namespace NHibernate.Search
{
    public class Environment
    {
        /// <summary>
        /// Enable listeners auto registration in Hibernate Annotations and EntityManager. Default to true.
        /// </summary>
        public const string AutoRegisterListeners = "hibernate.search.autoregister_listeners";

        /// <summary>
        /// Defines the indexing strategy, default <code>event</code>
        /// Other options <code>manual</code>
        /// </summary>
        public const string IndexingStrategy = "hibernate.search.indexing_strategy";

        /// <summary>
        /// Lucene analyzer
        /// </summary>
        public const string AnalyzerClass = "hibernate.search.analyzer";

        public const string WorkerPrefix = "hibernate.search.worker.";
        public const string WorkerScope = WorkerPrefix + "scope";
        public const string WorkerBackend = WorkerPrefix + "backend";
        public const string WorkerExecution = WorkerPrefix + "execution";

        /// <summary>
        /// Defines the maximum number of indexing operation batched per transaction
        /// </summary>
        public const string WorkerBatchSize = WorkerPrefix + "batch_size";

        /// <summary>
        /// Thread pool size, default 1
        /// </summary>
        /// <remarks>Only used when execution is async</remarks>
        public const string WorkerThreadPoolSize = WorkerPrefix + "threadpool_size";

        /// <summary>
        /// Size of the buffer queue (besides the thread pool size), default infinite
        /// </summary>
        /// <remarks>Only used when execution is async</remarks>
        public const string WorkerWorkQueueSize = WorkerPrefix + "buffer_queue.max";

        /// <summary>
        /// The reader prefix
        /// </summary>
        public const string ReaderPrefix = "hibernate.search.reader.";

        /// <summary>
        /// Define the strategy used.
        /// </summary>
        public const string ReaderStrategy = ReaderPrefix + "strategy";

        /// <summary>
        /// Filter caching strategy class (must have a no-arg constructor and implements <see cref="IFilterCachingStrategy" />)
        /// </summary>
        public const string FilterCachingStrategy = "hibernate.search.filter.cache_strategy";

        public const string SourceBase = "sourceBase";
        public const string Source = "source";
        public const string IndexBase = "indexBase";
    }
}