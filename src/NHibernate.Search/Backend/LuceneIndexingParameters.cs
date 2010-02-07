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
        private readonly ParameterSet batchIndexParameters;
        private readonly ParameterSet transactionIndexParameters;

        #region Constructors

        /// <summary>
        /// Constructor which instantiates a new parameter object with the the default values.
        /// </summary>    
        public LuceneIndexingParameters()
        {
            batchIndexParameters = new ParameterSet();
            transactionIndexParameters = new ParameterSet();
        }

        #endregion

        #region Property methods

        public ParameterSet BatchIndexParameters
        {
            get { return batchIndexParameters; }
        }
   
        public ParameterSet TransactionIndexParameters
        {
            get { return transactionIndexParameters; }
        }

        #endregion
    }
}