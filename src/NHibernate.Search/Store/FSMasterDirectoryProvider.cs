using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using log4net;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
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
        private static readonly ILog log = LogManager.GetLogger(typeof(FSMasterDirectoryProvider));
        private int current;
        private FSDirectory directory;
        private String indexName;
        private ISearchFactoryImplementor searchFactory;
        private Timer timer;

        // Variables needed between initialize and start
        private string source;
        private DirectoryInfo indexDir;
        private string directoryProviderName;
        private IDictionary properties;

        public Directory Directory
        {
            get { return directory; }
        }

        public void Initialize(String directoryProviderName, IDictionary properties, ISearchFactoryImplementor searchFactory)
        {
            this.properties = properties;
            this.directoryProviderName = directoryProviderName;
            //source guessing
            source = DirectoryProviderHelper.GetSourceDirectory(Environment.SourceBase, Environment.Source, directoryProviderName, properties);
            if (source == null)
                throw new ArgumentException("FSMasterDirectoryProvider requires a viable source directory");
            log.Debug("Source directory: " + source);
            indexDir = DirectoryProviderHelper.DetermineIndexDir(directoryProviderName, properties);
            log.Debug("Index directory: " + indexDir);
            try
            {
                bool create = !File.Exists(Path.Combine(indexDir.FullName, "segments"));
                create = !indexDir.Exists;
                if (create)
                {
                    log.Debug("Index directory '" + indexName + "' will be initialized");
                    indexDir.Create();
                }
                indexName = indexDir.FullName;
                directory = FSDirectory.GetDirectory(indexName, create);

                if (create)
                {
                    indexName = indexDir.FullName;
                    IndexWriter iw = new IndexWriter(directory, new StandardAnalyzer(), create);
                    iw.Close();
                }

                //copy to source
                if (File.Exists(Path.Combine(source, "current1")))
                    current = 2;
                else if (File.Exists(Path.Combine(source, "current2")))
                    current = 1;
                else
                {
                    log.Debug("Source directory for '" + indexName + "' will be initialized");
                    current = 1;
                }
                String currentString = current.ToString();
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
            //searchFactory.RegisterDirectoryProviderForLocks(this);
            //timer = new Timer(new CopyDirectory(this, indexName, source).Run);
            //timer.Change(period, period);
            this.searchFactory = searchFactory;
        }

        public void Start()
        {
            
        }

        public override bool Equals(Object obj)
        {
            // this code is actually broken since the value change after initialize call
            // but from a practical POV this is fine since we only call this method
            // after initialize call
            if (obj == this) return true;
            if (obj == null || !(obj is FSMasterDirectoryProvider)) return false;
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

        #region Nested type: CopyDirectory

        private class CopyDirectory
        {
            private readonly string source;
            private readonly string destination;
            private readonly FSMasterDirectoryProvider parent;
            private IDirectoryProvider directoryProvider;
            private bool inProgress;
            private object directoryProviderLock;

            public CopyDirectory(FSMasterDirectoryProvider parent, string source, string destination, IDirectoryProvider directoryProvider)
            {
                this.parent = parent;
                this.source = source;
                this.destination = destination;
                this.directoryProvider = directoryProvider;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public void Run(object ignored)
            {
                //TODO get rid of current and use the marker file instead?
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
                        //TODO make smart a parameter
                        try
                        {
                            log.Info("Copying " + sourceFile + " into " + destinationFile);
                            FileHelper.Synchronize(sourceFile, destinationFile, true);
                            parent.current = index;
                        }
                        catch (IOException e)
                        {
                            //don't change current
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
                log.Info("Copy for " + parent.indexName + " took " + (DateTime.Now - start) + ".");
            }
        }

        #endregion
    }
}