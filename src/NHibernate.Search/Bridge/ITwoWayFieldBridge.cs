using Lucene.Net.Documents;

namespace NHibernate.Search.Bridge
{
    /// <summary>
    /// An IFieldBridge able to convert the index representation back into an object without losing information
    /// 
    /// Any bridge expected to process a document id should implement this interface
    /// EXPERIMENTAL Consider this interface as private
    /// </summary>
    /// TODO: rework the interface inheritance there are some common concepts with StringBridge
    public interface ITwoWayFieldBridge : IFieldBridge
    {
        /// <summary>
        /// Build the element object from the Document
        /// </summary>
        /// <param name="value"></param>
        /// <param name="document"></param>
        /// <returns>The return value is the Entity id</returns>
        object Get(string value, Document document);

        /// <summary>
        /// Convert the object representation to a String
        /// The return String must not be null, it can be empty though
        /// EXPERIMENTAL API subject to change in the future
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        string ObjectToString(object obj);
    }
}