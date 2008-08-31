namespace NHibernate.Search
{
    public static class ProjectionConstants
    {
        /// <summary>
        /// Represents the Hibernate Entity returned in a search.
        /// </summary>
        public const string THIS = "__HSearch_This";

        /// <summary>
        /// The Lucene document returned by a search.
        /// </summary>
        public const string DOCUMENT = "__HSearch_Document";

        /// <summary>
        /// The legacy document's score from a search.
        /// </summary>
        public const string SCORE = "__HSearch_Score";

        /// <summary>
        /// The boost value of the Lucene document.
        /// </summary>
        public const string BOOST = "__HSearch_Boost";

        /// <summary>
        /// Object id property
        /// </summary>
        public const string ID = "__HSearch_id";

        /// <summary>
        /// Lucene Document id
        /// </summary>
        /// <remarks>
        /// Experimental: If you use this feature, please speak up in the forum
        /// Expert: Lucene document id can change overtime between 2 different IndexReader opening.
        /// </remarks>
        public const string DOCUMENT_ID = "__HSearch_DocumentId";
    }
}