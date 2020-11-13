using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NHibernate.Search.Engine;
using Directory=Lucene.Net.Store.Directory;

namespace NHibernate.Search.Store
{
    public class RAMDirectoryProvider : IDirectoryProvider
    {
        private RAMDirectory _directory;
        private string indexName;

        public Directory Directory => _directory;

        public void Initialize(String directoryProviderName, IDictionary<string, string> properties, ISearchFactoryImplementor searchFactory)
        {
            if (directoryProviderName == null)
                throw new ArgumentNullException("directoryProviderName");

            indexName = directoryProviderName;
            _directory = new RAMDirectory();
            try
            {
                var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
                var iw = new IndexWriter(_directory, config);
                iw.Dispose();
                //searchFactory.RegisterDirectoryProviderForLocks(this);
            }
            catch (IOException e)
            {
                throw new HibernateException("Unable to initialize index: " + indexName, e);
            }
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
            if (obj == null || !(obj is RAMDirectoryProvider)) return false;
            return indexName.Equals(((RAMDirectoryProvider) obj).indexName);
        }

        public override int GetHashCode()
        {
            // this code is actually broken since the value change after initialize call
            // but from a practical POV this is fine since we only call this method
            // after initialize call
            int hash = 7;
            return 29*hash + indexName.GetHashCode();
        }
    }
}