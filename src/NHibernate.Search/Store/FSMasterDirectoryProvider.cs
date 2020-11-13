using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NHibernate.Search.Engine;
using Directory=Lucene.Net.Store.Directory;

namespace NHibernate.Search.Store
{
    /// <summary>
    /// File based DirectoryProvider that takes care of index copy
    /// The base directory is represented by hibernate.search.<index>.indexBase
    /// The index is created in <base directory>/<index name>
    /// The source (aka copy) directory is built from <sourceBase>/<index name>
    /// A copy is triggered every refresh seconds
    /// </summary>
    public class FSMasterDirectoryProvider : IDirectoryProvider
    {
		private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(FSMasterDirectoryProvider));
        private FSDirectory directory;
        private int current;
        private string indexName;
        private Timer timer;
        private ISearchFactoryImplementor searchFactory;
        private TriggerTask task;

        // Variables needed between initialize and start
        private string source;
        private DirectoryInfo indexDir;
        private string directoryProviderName;
        private IDictionary<string, string> properties;

        #region Destructor

        ~FSMasterDirectoryProvider()
        {
            if (task != null)
            {
                task.Abandon = true;
            }

            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        #endregion

        #region Property methods

        public Directory Directory
        {
            get { return directory; }
        }

        #endregion

        #region Public methods

        public void Initialize(string directoryProviderName, IDictionary<string, string> properties, ISearchFactoryImplementor searchFactory)
        {
            this.properties = properties;
            this.directoryProviderName = directoryProviderName;

            // source guessing
            source = DirectoryProviderHelper.GetSourceDirectory(Environment.SourceBase, Environment.Source, directoryProviderName, (IDictionary) properties);
            if (source == null)
            {
                throw new ArgumentException("FSMasterDirectoryProvider requires a viable source directory");
            }

            log.Debug("Source directory: " + source);
            indexDir = DirectoryProviderHelper.DetermineIndexDir(directoryProviderName, (IDictionary) properties);
            indexName = indexDir.FullName;
            log.Debug("Index directory: " + indexDir);
            try
            {
                // NB Do we need to do this since we are passing the create flag to Lucene?
                directory = FSDirectory.Open(indexName);
                if (!DirectoryReader.IndexExists(directory))
                {
                    log.DebugFormat("Index directory not found, creating '{0}'", indexDir.FullName);
                    indexDir.Create();
                    indexName = indexDir.FullName;
                    var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
                    IndexWriter iw = new IndexWriter(directory, config);
                    iw.Dispose();
                }
            }
            catch (IOException e)
            {
                throw new HibernateException("Unable to initialize index: " + directoryProviderName, e);
            }

            this.searchFactory = searchFactory;
        }

        public void Start()
        {
            string refreshPeriod = properties.ContainsKey("refresh") ? properties["refresh"] : "3600";
            long period;
            if (!long.TryParse(refreshPeriod, out period))
            {                
                period = 3600;
                log.Warn("Error parsing refresh period, defaulting to 1 hour");
            }

            log.DebugFormat("Refresh period {0} seconds", period);            
            period *= 1000;  // per second

            try
            {
                // Copy to source
                if (File.Exists(Path.Combine(source, "current1")))
                {
                    current = 2;
                }
                else if (File.Exists(Path.Combine(source, "current2")))
                {
                    current = 1;
                }
                else
                {
                    log.DebugFormat("Source directory for '{0}' will be initialized", indexName);
                    current = 1;
                }

                string currentString = current.ToString();
                DirectoryInfo subDir = new DirectoryInfo(Path.Combine(source, currentString));
                FileHelper.Synchronize(indexDir, subDir, true);
                File.Delete(Path.Combine(source, "current1"));
                File.Delete(Path.Combine(source, "current2"));
                log.Debug("Current directory: " + current);
                
            }
            catch (IOException e)
            {
                throw new HibernateException("Unable to initialize index: " + directoryProviderName, e);
            }

            task = new TriggerTask(this, indexName, source);
            timer = new Timer(task.Run, null, period, period);
        }

        public override bool Equals(object obj)
        {
            // this code is actually broken since the value change after initialize call
            // but from a practical POV this is fine since we only call this method
            // after initialize call
            if (obj == this)
            {
                return true;
            }

            if (obj == null || !(obj is FSMasterDirectoryProvider))
            {
                return false;
            }

            return indexName.Equals(((FSMasterDirectoryProvider) obj).indexName);
        }

        public override int GetHashCode()
        {
            // this code is actually broken since the value change after initialize call
            // but from a practical POV this is fine since we only call this method
            // after initialize call
            int hash = 11;
            return 37*hash + indexName.GetHashCode();
        }

        #endregion

        #region Nested type: CopyDirectory

        private class CopyDirectory
        {
            private readonly string source;
            private readonly string destination;
            private readonly FSMasterDirectoryProvider parent;
            private IDirectoryProvider directoryProvider;
            private bool inProgress;
            private object directoryProviderLock;

            #region Constructors

            public CopyDirectory(FSMasterDirectoryProvider parent, string source, string destination, IDirectoryProvider directoryProvider)
            {
                this.parent = parent;
                this.source = source;
                this.destination = destination;
                this.directoryProvider = directoryProvider;
            }

            #endregion

            #region Property methods

            /// <summary>
            /// Is the copy still executing
            /// </summary>
            public bool InProgress
            {
                get { return inProgress; }
            }

            #endregion

            #region Public methods

            //[MethodImpl(MethodImplOptions.Synchronized)]
            public void Run()
            {
                // TODO get rid of current and use the marker file instead?
                DateTime start = DateTime.Now;
                inProgress = true;
                if (directoryProviderLock == null)
                {
                    directoryProviderLock = parent.searchFactory.GetLockableDirectoryProviders()[directoryProvider];
                    directoryProvider = null;
                }

                try
                {
                    lock (directoryProviderLock)
                    {
                        int oldIndex = parent.current;
                        int index = parent.current == 1 ? 2 : 1;
                        DirectoryInfo sourceFile = new DirectoryInfo(source);
                        DirectoryInfo destinationFile = new DirectoryInfo(Path.Combine(destination, index.ToString()));

                        // TODO make smart a parameter
                        try
                        {
                            log.Info("Copying " + sourceFile + " into " + destinationFile);
                            FileHelper.Synchronize(sourceFile, destinationFile, true);
                            parent.current = index;
                        }
                        catch (IOException e)
                        {
                            // Don't change current
                            log.Error("Unable to synchronize source of " + parent.indexName, e);
                            return;
                        }

                        try
                        {
                            File.Delete(Path.Combine(destination, "current" + oldIndex));
                        }
                        catch (IOException e)
                        {
                            log.Warn("Unable to remove previous marker file from source of " + parent.indexName, e);
                        }

                        try
                        {
                            File.Create(Path.Combine(destination, "current" + index)).Dispose();
                        }
                        catch (IOException e)
                        {
                            log.Warn("Unable to create current marker in source of " + parent.indexName, e);
                        }
                    }
                }
                finally
                {
                    inProgress = false;
                }

                log.InfoFormat("Copy for {0} took {1}.", parent.indexName, (DateTime.Now - start));
            }

            #endregion
        }

        #endregion

        #region Nested type: TriggerTask

        private class TriggerTask
        {
            private readonly CopyDirectory copyTask;
            private readonly string source;
            private bool abandon;

            public TriggerTask(FSMasterDirectoryProvider parent, string source, string destination)
            {
                abandon = false;
                this.source = source;
                copyTask = new CopyDirectory(parent, source, destination, parent);
            }

            /// <summary>
            /// 
            /// </summary>
            public bool Abandon
            {
                set { abandon = value; }
            }

            public void Run(object ignore)
            {
                // We are in wind down mode, don't bother any more
                if (abandon)
                {
                    return;
                }

                if (!copyTask.InProgress)
                {
                    copyTask.Run();
                }
                else
                {
                    log.Info("Skipping directory synchronization, previous work still in progress: " + source);
                }
            }
        }

        #endregion
    }
}