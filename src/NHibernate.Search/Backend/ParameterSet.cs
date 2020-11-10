namespace NHibernate.Search.Backend
{
    using System;

    using Lucene.Net.Index;

    public class ParameterSet
    {
        public int? MergeFactor { get; set; }

        public int? MaxMergeDocs { get; set; }

        public int? MaxBufferedDocs { get; set; }

        public int? TermIndexInterval { get; set; }

        public int? RamBufferSizeMb { get; set; }

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
            catch (ArgumentOutOfRangeException)
            {
                // TODO: Log it
            }
        }
    }
}
