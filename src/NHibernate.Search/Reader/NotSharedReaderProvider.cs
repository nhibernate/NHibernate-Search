using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Search.Store;

namespace NHibernate.Search.Reader
{
    /// <summary>
    /// Open a reader each time
    /// </summary>
    public class NotSharedReaderProvider : IReaderProvider
    {
        public IndexReader OpenReader(IDirectoryProvider[] directoryProviders)
        {
            int length = directoryProviders.Length;
            IndexReader[] readers = new IndexReader[length];
            try
            {
                for (int index = 0; index < length; index++)
                    readers[index] = IndexReader.Open(directoryProviders[index].Directory);
            }
            catch (System.IO.IOException ex)
            {
                // TODO: more contextual info
                ReaderProviderHelper.Clean(readers);
                throw new SearchException("Unable to open one of the Lucene indexes", ex);
            }

            return ReaderProviderHelper.BuildMultiReader(length, readers);
        }

        public void CloseReader(IndexReader reader)
        {
            try
            {
                reader.Dispose();
            }
            catch (System.IO.IOException ex)
            {
                //TODO: extract subReaders and close each one individually
                ReaderProviderHelper.Clean(reader);
                new SearchException("Unable to close multiReader", ex);
            }
        }

        public void Initialize(IDictionary<string, string> properties,
                               ISearchFactoryImplementor searchFactoryImplementor)
        {
        }

        public void Destroy()
        {
        }
    }
}