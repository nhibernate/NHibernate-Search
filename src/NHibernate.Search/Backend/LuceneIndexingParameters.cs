namespace NHibernate.Search.Backend
{
    /// <summary>
    /// Wrapper class around the Lucene indexing parameters <i>mergeFactor</i>, <i>maxMergeDocs</i> and
    /// <i>maxBufferedDocs</i>.
    /// <p>
    /// There are two sets of these parameters. One is for regular indexing the other is for batch indexing
    /// triggered by <code>FullTextSessoin.Index(Object entity)</code>
    /// </summary>
    public class LuceneIndexingParameters
    {
        private const int DEFAULT_MAX_BUFFERED_DOCS = 10;
        private const int DEFAULT_MAX_MERGE_DOCS = int.MinValue;
        private const int DEFAULT_MERGE_FACTOR = 10;
        private const int DEFAULT_RAM_BUFFER = 64;

        private int batchMaxBufferedDocs = DEFAULT_MAX_BUFFERED_DOCS;
        private int batchMaxMergeDocs = int.MinValue;
        private int batchMergeFactor = DEFAULT_MERGE_FACTOR;
        private int batchRamBufferSizeMb = DEFAULT_RAM_BUFFER;
        private int transactionMaxBufferedDocs = DEFAULT_MAX_BUFFERED_DOCS;
        private int transactionMaxMergeDocs = int.MaxValue;
        private int transactionMergeFactor = DEFAULT_MERGE_FACTOR;
        private int transactionRamBufferSizeMb = DEFAULT_RAM_BUFFER;

        #region Constructors

        /// <summary>
        /// Constructor which instantiates a new parameter object with the the default values.
        /// </summary>    
        public LuceneIndexingParameters()
        {
            transactionMergeFactor = DEFAULT_MERGE_FACTOR;
            batchMergeFactor = DEFAULT_MERGE_FACTOR;
            transactionMaxMergeDocs = DEFAULT_MAX_MERGE_DOCS;
            batchMaxMergeDocs = DEFAULT_MAX_MERGE_DOCS;
            transactionMaxBufferedDocs = DEFAULT_MAX_BUFFERED_DOCS;
            batchMaxBufferedDocs = DEFAULT_MAX_BUFFERED_DOCS;
        }

        #endregion

        #region Property methods

        public int BatchMaxMergeDocs
        {
            get { return batchMaxMergeDocs; }
            set { batchMaxMergeDocs = value; }
        }

        public int BatchMergeFactor
        {
            get { return batchMergeFactor; }
            set { batchMergeFactor = value; }
        }

        public int BatchMaxBufferedDocs
        {
            get { return batchMaxBufferedDocs; }
            set { batchMaxBufferedDocs = value; }
        }

        public int BatchRamBufferSizeMb
        {
            get { return batchRamBufferSizeMb;  }
            set { batchRamBufferSizeMb = value; }
        }

        public int TransactionMaxBufferedDocs
        {
            get { return transactionMaxBufferedDocs; }
            set { transactionMaxBufferedDocs = value; }
        }

        public int TransactionMaxMergeDocs
        {
            get { return transactionMaxMergeDocs; }
            set { transactionMaxMergeDocs = value; }
        }

        public int TransactionMergeFactor
        {
            get { return transactionMergeFactor; }
            set { transactionMergeFactor = value; }
        }

        public int TransactionRamBufferSizeMb
        {
            get { return transactionRamBufferSizeMb; }
            set { transactionRamBufferSizeMb = value; }
        }

        #endregion
    }
}