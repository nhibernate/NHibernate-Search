using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Iesi.Collections.Generic;
using Lucene.Net.Index;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Search.Store;
using FieldInfo = System.Reflection.FieldInfo;

namespace NHibernate.Search.Reader
{
    /// <summary>
    /// Share readers per SearchFactory, reusing them iff they are still valid.
    /// </summary>
    public class SharedReaderProvider : IReaderProvider
    {
        private static readonly INHibernateLogger log = NHibernateLogger.For(typeof(SharedReaderProvider));
        private static FieldInfo subReadersField;

        /// <summary>
        /// Contain the active (ie non obsolete IndexReader for a given Directory
        /// There may be no entry (warm up)
        /// <p/>
        /// protected by semaphoreIndexReaderLock
        /// </summary>
        private readonly Dictionary<IDirectoryProvider, IndexReader> activeSearchIndexReaders = new Dictionary<IDirectoryProvider, IndexReader>();

        /// <summary>
        /// contains the semaphore and the directory provider per IndexReader opened
        /// all read / update have to be protected by semaphoreIndexReaderLock
        /// </summary>
        private readonly Dictionary<IndexReader, ReaderData> searchIndexReaderSemaphores = new Dictionary<IndexReader, ReaderData>();

        /// <summary>
        /// nonfair lock. Need to be acquired on indexReader acquisition or release (semaphore)
        /// </summary>
        private readonly object semaphoreIndexReaderLock = new object();

        /// <summary>
        /// non fair list of locks to block per IndexReader only
        /// Locks have to be acquired at least for indexReader retrieval and switch
        /// ie for all activeSearchIndexReaders manipulation
        /// this map is read only after initialization, no need to synchronize
        /// </summary>
        private Dictionary<IDirectoryProvider, object> perDirectoryProviderManipulationLocks;

        #region Private methods

        private IndexReader ReplaceActiveReader(IndexReader outOfDateReader, object directoryProviderLock,
                                                IDirectoryProvider directoryProvider, IndexReader[] readers)
        {
            bool trace = log.IsInfoEnabled();
            IndexReader oldReader;
            bool closeOldReader = false;
            bool closeOutOfDateReader = false;
            IndexReader reader;
            /**
             * Since out of lock protection, can have multiple readers created in //
             * not worse than NotShared and limit the locking time, hence scalability
             */
            try
            {
                reader = DirectoryReader.Open(directoryProvider.Directory);
            }
            catch (IOException e)
            {
                throw new SearchException("Unable to open Lucene IndexReader", e);
            }
            lock (directoryProviderLock)
            {
                // Since not protected by a lock, other ones can have been added
                oldReader = activeSearchIndexReaders[directoryProvider] = reader;
                lock (semaphoreIndexReaderLock)
                {
                    searchIndexReaderSemaphores[reader] = new ReaderData(1, directoryProvider);
                    if (trace) log.Info("Semaphore: 1 for " + reader);
                    if (outOfDateReader != null)
                    {
                        ReaderData readerData;
                        searchIndexReaderSemaphores.TryGetValue(outOfDateReader, out readerData);
                        if (readerData == null)
                        {
                            closeOutOfDateReader = false; //already removed by another prevous thread
                        }
                        else if (readerData.Semaphore == 0)
                        {
                            searchIndexReaderSemaphores.Remove(outOfDateReader);
                            closeOutOfDateReader = true;
                        }
                        else
                        {
                            closeOutOfDateReader = false;
                        }
                    }

                    if (oldReader != null && oldReader != outOfDateReader)
                    {
                        ReaderData readerData = searchIndexReaderSemaphores[oldReader];
                        if (readerData == null)
                        {
                            log.Warn("Semaphore should not be null");
                            closeOldReader = true; //TODO should be true or false?
                        }
                        else if (readerData.Semaphore == 0)
                        {
                            searchIndexReaderSemaphores.Remove(oldReader);
                            closeOldReader = true;
                        }
                        else
                        {
                            closeOldReader = false;
                        }
                    }
                }
            }

            if (closeOutOfDateReader)
            {
                if (trace) log.Info("Closing out of date IndexReader " + outOfDateReader);
                try
                {
                    outOfDateReader.Dispose();
                }
                catch (IOException e)
                {
                    ReaderProviderHelper.Clean(readers);
                    throw new SearchException("Unable to close Lucene IndexReader", e);
                }
            }
            if (closeOldReader)
            {
                if (trace) log.Info("Closing old IndexReader " + oldReader);
                try
                {
                    oldReader.Dispose();
                }
                catch (IOException e)
                {
                    ReaderProviderHelper.Clean(readers);
                    throw new SearchException("Unable to close Lucene IndexReader", e);
                }
            }
            return reader;
        }

        #endregion

        #region Public methods

        public IndexReader OpenReader(IDirectoryProvider[] directoryProviders)
        {
            bool trace = log.IsInfoEnabled();
            int length = directoryProviders.Length;
            IndexReader[] readers = new IndexReader[length];

            if (trace) log.Info("Opening IndexReader for directoryProviders: " + length);

            for (int index = 0; index < length; index++)
            {
                IDirectoryProvider directoryProvider = directoryProviders[index];
                IndexReader reader;
                object directoryProviderLock = perDirectoryProviderManipulationLocks[directoryProvider];
                if (trace) log.Info("Opening IndexReader from " + directoryProvider.Directory);
                lock (directoryProviderLock)
                {
                    activeSearchIndexReaders.TryGetValue(directoryProvider, out reader);
                }
                if (reader == null)
                {
                    if (trace)
                        log.Info("No shared IndexReader, opening a new one: " + directoryProvider.Directory);
                    reader = ReplaceActiveReader(null, directoryProviderLock, directoryProvider, readers);
                }
                else
                {
                    bool isCurrent;
                    try
                    {
                        isCurrent = (reader as DirectoryReader)?.IsCurrent() ?? false;
                    }
                    catch (IOException e)
                    {
                        throw new SearchException("Unable to read current status of Lucene IndexReader", e);
                    }

                    if (!isCurrent)
                    {
                        if (trace)
                            log.Info("Out of date shared IndexReader found, opening a new one: " +
                                     directoryProvider.Directory);
                        IndexReader outOfDateReader = reader;
                        reader = ReplaceActiveReader(outOfDateReader, directoryProviderLock, directoryProvider, readers);
                    }
                    else
                    {
                        if (trace)
                            log.Info("Valid shared IndexReader: " + directoryProvider.Directory);

                        lock (directoryProviderLock)
                        {
                            //read the latest active one, the current one could be out of date and closed already
                            //the latest active is guaranteed to be active because it's protected by the dp lock
                            reader = activeSearchIndexReaders[directoryProvider];
                            lock (semaphoreIndexReaderLock)
                            {
                                ReaderData readerData = searchIndexReaderSemaphores[reader];
                                //TODO if readerData is null????
                                readerData.Semaphore++;
                                searchIndexReaderSemaphores[reader] = readerData; //not necessary
                                if (trace) log.Info("Semaphore increased: " + readerData.Semaphore + " for " + reader);
                            }
                        }
                    }
                }
                readers[index] = reader;
            }

            return ReaderProviderHelper.BuildMultiReader(length, readers);
        }

        public void CloseReader(IndexReader reader)
        {
            bool trace = log.IsInfoEnabled();
            if (reader == null) return;
            IndexReader[] readers;

            // TODO: Java says don't force this to be CacheableMultiReader, but if we do we could avoid the reflection
            if (reader is BaseCompositeReader<IndexReader>)
            {
                try
                {
                    // TODO: Need to account for Medium Trust - can't reflect on private members
                    readers = (IndexReader[])subReadersField.GetValue(reader);
                }
                catch (Exception e)
                {
                    throw new SearchException("Incompatible version of Lucene: MultiReader.subReaders not accessible", e);
                }
                if (trace) log.Info("Closing MultiReader: " + reader);
            }
            else
            {
                throw new AssertionFailure("Everything should be wrapped in a MultiReader");
            }

            foreach (IndexReader subReader in readers)
                CloseInternalReader(trace, subReader, false);
        }

        private void CloseInternalReader(bool trace, IndexReader subReader, bool finalClose)
        {
            ReaderData readerData;
            // TODO: can we avoid the lock?
            lock (semaphoreIndexReaderLock)
            {
                readerData = searchIndexReaderSemaphores[subReader];
            }

            if (readerData == null)
            {
                log.Error("Trying to close a Lucene IndexReader not present: " + (subReader as DirectoryReader)?.Directory);
                // TODO: Should we try to close?
                return;
            }

            // Acquire the locks in the same order as everywhere else
            object directoryProviderLock = perDirectoryProviderManipulationLocks[readerData.Provider];
            bool closeReader = false;
            lock (directoryProviderLock)
            {
                IndexReader reader;
                bool isActive = activeSearchIndexReaders.TryGetValue(readerData.Provider, out reader)
                    && reader == subReader;
                if (trace) log.Info("IndexReader not active: " + subReader);
                lock (semaphoreIndexReaderLock)
                {
                    readerData = searchIndexReaderSemaphores[subReader];
                    if (readerData == null)
                    {
                        log.Error("Trying to close a Lucene IndexReader not present: " + (subReader as DirectoryReader)?.Directory);
                        // TODO: Should we try to close?
                        return;
                    }

                    //final close, the semaphore should be at 0 already
                    if (!finalClose)
                    {
                        readerData.Semaphore--;
                        if (trace)
                            log.Info("Semaphore decreased to: " + readerData.Semaphore + " for " + subReader);
                    }

                    if (readerData.Semaphore < 0)
                        log.Error("Semaphore negative: " + (subReader as DirectoryReader)?.Directory);

                    if (!isActive && readerData.Semaphore == 0)
                    {
                        searchIndexReaderSemaphores.Remove(subReader);
                        closeReader = true;
                    }
                    else
                        closeReader = false;
                }
            }

            if (closeReader)
            {
                if (trace) log.Info("Closing IndexReader: " + subReader);
                try
                {
                    subReader.Dispose();
                }
                catch (IOException e)
                {
                    log.Warn(e, "Unable to close Lucene IndexReader");
                }
            }
        }

        public void Initialize(IDictionary<string, string> properties,
                               ISearchFactoryImplementor searchFactoryImplementor)
        {
            if (subReadersField == null)
            {
                // TODO: If we check for CacheableMultiReader we could avoid reflection here!
                // TODO: Need to account for Medium Trust - can't reflect on private members
                subReadersField = typeof(BaseCompositeReader<IndexReader>).GetField("subReaders",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }

            HashSet<IDirectoryProvider> providers =
                new HashSet<IDirectoryProvider>(searchFactoryImplementor.GetLockableDirectoryProviders().Keys);
            perDirectoryProviderManipulationLocks = new Dictionary<IDirectoryProvider, object>();
            foreach (IDirectoryProvider dp in providers)
                perDirectoryProviderManipulationLocks[dp] = new object();
        }

        public void Destroy()
        {
            bool trace = log.IsInfoEnabled();
            List<IndexReader> readers;
            lock (semaphoreIndexReaderLock)
            {
                //release active readers
                activeSearchIndexReaders.Clear();
                readers = new List<IndexReader>();
                readers.AddRange(searchIndexReaderSemaphores.Keys);
            }

            foreach (IndexReader reader in readers)
            {
                CloseInternalReader(trace, reader, true);
            }

            if (searchIndexReaderSemaphores.Count != 0)
            {
                log.Warn("ReaderProvider contains readers not properly closed at destroy time");
            }
        }

        #endregion

        #region Nested classes: ReaderData

        private class ReaderData
        {
            public readonly IDirectoryProvider Provider;
            public int Semaphore;

            public ReaderData(int semaphore, IDirectoryProvider provider)
            {
                Semaphore = semaphore;
                Provider = provider;
            }
        }

        #endregion
    }
}