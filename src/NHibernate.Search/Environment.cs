namespace NHibernate.Search {
    public class Environment {
        public const string AnalyzerClass = "hibernate.search.analyzer";
        public const string DirectoryProvider = "directory_provider";
        public const string IndexBase = "indexBase";

        /**
		 * Defines the indexing strategy, default <code>event</code>
		 * Other options <code>manual</code>
		 */
        public const string IndexingStrategy = "hibernate.search.indexing_strategy";
        public const string Refresh = "refresh";
        public const string Source = "source";
        public const string SourceBase = "sourceBase";
        public const string WorkerBackend = "hibernate.search.worker.backend";
        public const string WorkerBatchSize = "hibernate.search.worker.batch_size";
        public const string WorkerExecution = "hibernate.search.worker.execution";
    }
}