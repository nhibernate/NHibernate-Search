using Lucene.Net.Index.Extensions;

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

        public void ApplyToWriterConfig(IndexWriterConfig config)
        {
            try
            {
                if (MergeFactor != null)
                {
                    config.SetMergeFactor((int) MergeFactor);
                }

                if (MaxMergeDocs != null)
                {
                    config.SetMaxMergeDocs((int) MaxMergeDocs);
                }

                if (MaxBufferedDocs != null)
                {
                    config.SetMaxBufferedDocs((int) MaxBufferedDocs);
                }

                if (RamBufferSizeMb != null)
                {
                    config.SetRAMBufferSizeMB((int) RamBufferSizeMb);
                }

                if (TermIndexInterval != null)
                {
                    config.SetTermIndexInterval((int) TermIndexInterval);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // TODO: Log it
            }
        }
    }
}
