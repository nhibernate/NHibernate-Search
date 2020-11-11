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
    /// File based directory provider that takes care of geting a version of the index
    /// from a given source
    /// The base directory is represented by hibernate.search.<index>.indexBase
    /// The index is created in <base directory>/<index name>
    /// The source (aka copy) directory is built from <sourceBase>/<index name>
    /// 
    /// A copy is triggered every refresh seconds
    /// </summary>
    public class FSSlaveDirectoryProvider : IDirectoryProvider
    {
        private const LuceneVersion _luceneVersion = LuceneVersion.LUCENE_48;
		private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(FSSlaveDirectoryProvider));
        private FSDirectory directory1;
        private FSDirectory directory2;
        private string indexName;
        private int current;
        private Timer timer;
        private TriggerTask task;

        // variables needed between initialize and start
        private string source;
        private DirectoryInfo indexDir;
        private string directoryProviderName;
        private IDictionary<string, string> properties;

        #region Constructor/destructor

        ~FSSlaveDirectoryProvider()
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
            get
            {
                switch (current)
                {
                    case 1:
                        return directory1;
                    case 2:
                        return directory2;
                    default:
                        throw new AssertionFailure("Illegal current directory: " + current);
                }
            }
        }

        #endregion

        #region Public methods

        public void Initialize(String directoryProviderName, IDictionary<string, string> properties, ISearchFactoryImplementor searchFactory)
        {
            this.properties = properties;
            this.directoryProviderName = directoryProviderName;

            // source guessing
            source = DirectoryProviderHelper.GetSourceDirectory(Environment.SourceBase, Environment.Source, directoryProviderName, (IDictionary) properties);
            if (source == null)
            {
                throw new ArgumentException("FSSlaveDirectoryProvider requires a viable source directory");
            }

            if (!File.Exists(Path.Combine(source, "current1")) && !File.Exists(Path.Combine(source, "current2")))
            {
                log.Warn("No current marker in source directory: " + source);
            }

            log.Debug("Source directory: " + source);
            indexDir = DirectoryProviderHelper.DetermineIndexDir(directoryProviderName, (IDictionary) properties);
            log.Debug("Index directory: " + indexDir.FullName);
            try
            {
                bool create = !indexDir.Exists;
                if (create)
                {
                    log.DebugFormat("Index directory not found, creating '{0}'", indexDir.FullName);
                    indexDir.Create();
                }
                indexName = indexDir.FullName;
            }
            catch (IOException e)
            {
                throw new HibernateException("Unable to initialize index: " + directoryProviderName, e);
            }
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
                var config = new IndexWriterConfig(_luceneVersion, new StandardAnalyzer(_luceneVersion));

                var indexPath1 = new DirectoryInfo(Path.Combine(indexName, "1"));
                directory1 = FSDirectory.Open(indexPath1);
                if (!DirectoryReader.IndexExists(directory1))
                {
                    log.DebugFormat("Initialize index: '{0}'", indexPath1);
                    var iw1 = new IndexWriter(directory1, config);
                    try
                    {
                        iw1.Dispose();
                    }
                    finally
                    {
                        if (IndexWriter.IsLocked(directory1))
                        {
                            IndexWriter.Unlock(directory1);
                        }
                    }
                }

                var indexPath2 = Path.Combine(indexName, "2");
                directory2 = FSDirectory.Open(indexPath2);
                if (!DirectoryReader.IndexExists(directory2))
                {
                    log.DebugFormat("Initialize index: '{0}'", indexPath2);
                    var iw2 = new IndexWriter(directory2, config);
                    try
                    {
                        iw2.Dispose();
                    }
                    finally
                    {
                        if (IndexWriter.IsLocked(directory2))
                        {
                            IndexWriter.Unlock(directory2);
                        }
                    }
                }

                string current1Marker = Path.Combine(indexName, "current1");
                string current2Marker = Path.Combine(indexName, "current2");
                if (File.Exists(current1Marker))
                {
                    current = 1;
                }
                else if (File.Exists(current2Marker))
                {
                    current = 2;
                }
                else
                {
                    // no default
                    log.Debug("Setting directory 1 as current");
                    current = 1;
                    DirectoryInfo srcDir = new DirectoryInfo(source);
                    DirectoryInfo destDir = new DirectoryInfo(Path.Combine(indexName, current.ToString()));
                    int sourceCurrent = -1;
                    if (File.Exists(Path.Combine(srcDir.Name, "current1")))
                    {
                        sourceCurrent = 1;
                    }
                    else if (File.Exists(Path.Combine(srcDir.Name, "current2")))
                    {
                        sourceCurrent = 2;
                    }

                    if (sourceCurrent != -1)
                    {
                        try
                        {
                            FileHelper.Synchronize(
                                    new DirectoryInfo(Path.Combine(source, sourceCurrent.ToString())), destDir, true);
                        }
                        catch (IOException e)
                        {
                            throw new HibernateException("Umable to synchonize directory: " + indexName, e);
                        }
                    }

                    try
                    {
                        File.Create(current1Marker).Dispose();
                    }
                    catch (IOException e)
                    {
                        throw new HibernateException("Unable to create the directory marker file: " + indexName, e);
                    }
                }
                log.Debug("Current directory: " + current);
            }
            catch (IOException e)
            {
                throw new HibernateException("Unable to initialize index: " + directoryProviderName, e);
            }

            task = new TriggerTask(this, source, indexName);
            timer = new Timer(task.Run, null, period, period);
        }

        public override bool Equals(Object obj)
        {
            // this code is actually broken since the value change after initialize call
            // but from a practical POV this is fine since we only call this method
            // after initialize call
            if (obj == this) return true;
            if (obj == null || !(obj is FSSlaveDirectoryProvider)) return false;
            return indexName.Equals(((FSSlaveDirectoryProvider) obj).indexName);
        }

        public override int GetHashCode()
        {
            // this code is actually broken since the value change after initialize call
            // but from a practical POV this is fine since we only call this method
            // after initialize call
            const int hash = 11;
            return 37*hash + indexName.GetHashCode();
        }

        #endregion

        #region Nested type: CopyDirectory

        private class CopyDirectory
        {
            private readonly string destination;
            private readonly FSSlaveDirectoryProvider parent;
            private readonly string source;
            private volatile bool inProgress;

            public CopyDirectory(FSSlaveDirectoryProvider parent, string source, string destination)
            {
                this.parent = parent;
                this.source = source;
                this.destination = destination;
            }

            public bool InProgress
            {
                get { return inProgress; }
            }

            //[MethodImpl(MethodImplOptions.Synchronized)]
            public void Run()
            {
                DateTime start = DateTime.Now;
                try
                {
                    inProgress = true;
                    int oldIndex = parent.current;
                    int index = parent.current == 1 ? 2 : 1;
                    DirectoryInfo sourceFile;
                    string current1Slave = Path.Combine(source, "current1");
                    string current2Slave = Path.Combine(source, "current2");
                    if (File.Exists(current1Slave))
                    {
                        sourceFile = new DirectoryInfo(Path.Combine(source, "1"));
                    }
                    else if (File.Exists(current2Slave))
                    {
                        sourceFile = new DirectoryInfo(Path.Combine(source, "2"));
                    }
                    else
                    {
                        log.Warn("Unable to determine current in source directory");
                        inProgress = false;
                        return;
                    }

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
                        //don't change current
                        log.Error("Unable to synchronize " + parent.indexName, e);
                        inProgress = false;
                        return;
                    }

                    try
                    {
                        File.Delete(Path.Combine(parent.indexName, "current" + oldIndex));
                    }
                    catch (Exception e)
                    {
                        log.Warn("Unable to remove previous marker file in " + parent.indexName, e);
                    }

                    try
                    {
                        File.Create(Path.Combine(parent.indexName, "current" + index)).Dispose();
                    }
                    catch (IOException e)
                    {
                        log.Warn("Unable to create current marker file in " + parent.indexName, e);
                    }
                }
                finally
                {
                    inProgress = false;
                }
                log.Info("Copy for " + parent.indexName + " took " + (DateTime.Now - start) + ".");
            }
        }

        #endregion

        #region Nested type: TriggerTask

        private class TriggerTask
        {
            private readonly CopyDirectory copyTask;
            private readonly string source;
            private bool abandon;

            public TriggerTask(FSSlaveDirectoryProvider parent, string source, string destination)
            {
                abandon = false;
                this.source = source;
                copyTask = new CopyDirectory(parent, source, destination);
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