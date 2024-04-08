namespace NHibernate.Search.Backend
{
    using System;

    using Lucene.Net.Index;

    public class ParameterSet
    {
        private int? mergeFactor;
        private int? maxMergeDocs;
        private int? maxBufferedDocs;
        private int? termIndexInterval;
        private int? ramBufferSizeMb;

        public int? MergeFactor
        {
            get { return mergeFactor; }
            set { mergeFactor = value; }
        }

        public int? MaxMergeDocs
        {
            get { return maxMergeDocs; }
            set { maxMergeDocs = value; }
        }

        public int? MaxBufferedDocs
        {
            get { return maxBufferedDocs; }
            set { maxBufferedDocs = value; }
        }

        public int? TermIndexInterval
        {
            get { return termIndexInterval; }
            set { termIndexInterval = value; }
        }

        public int? RamBufferSizeMb
        {
            get { return ramBufferSizeMb; }
            set { ramBufferSizeMb = value; }
        }

        public void ApplyToWriterConfig(IndexWriterConfig writer)
        {
            try
            {
                var mergePolicy = new LogDocMergePolicy();
                if (MergeFactor.HasValue)
                {
                    mergePolicy.MergeFactor = MergeFactor.Value;
                }

                if (MaxMergeDocs.HasValue)
                {
                    mergePolicy.MaxMergeDocs = MaxMergeDocs.Value;
                }

                if (MergeFactor.HasValue || MaxMergeDocs.HasValue)
                {
                    writer.MergePolicy = mergePolicy;
                }

                if (MaxBufferedDocs.HasValue)
                {
                    writer.MaxBufferedDocs = MaxBufferedDocs.Value;
                }

                if (RamBufferSizeMb.HasValue)
                {
                    writer.RAMBufferSizeMB = RamBufferSizeMb.Value;
                }

                if (TermIndexInterval != null)
                {
                    writer.TermIndexInterval = (int)TermIndexInterval;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // TODO: Log it
            }
        }
    }
}
