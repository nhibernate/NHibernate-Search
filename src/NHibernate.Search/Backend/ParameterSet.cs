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

        public void ApplyToWriter(IndexWriter writer)
        {
            try
            {
                if (MergeFactor != null)
                {
                    writer.SetMergeFactor((int) MergeFactor);
                }

                if (MaxMergeDocs != null)
                {
                    writer.SetMaxMergeDocs((int) MaxMergeDocs);
                }

                if (MaxBufferedDocs != null)
                {
                    writer.SetMaxBufferedDocs((int) MaxBufferedDocs);
                }

                if (RamBufferSizeMb != null)
                {
                    writer.SetRAMBufferSizeMB((int) RamBufferSizeMb);
                }

                if (TermIndexInterval != null)
                {
                    writer.SetTermIndexInterval((int) TermIndexInterval);
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                // TODO: Log it
            }
        }
    }
}
