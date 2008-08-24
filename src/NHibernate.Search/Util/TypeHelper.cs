namespace NHibernate.Search.Util
{
    /// <summary>
    /// Utilities for types
    /// </summary>
    internal static class TypeHelper
    {
        /// <summary>
        /// Includes the name of the type and it's assembly so we load it, but are not tied to the specific version
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string LuceneTypeName(System.Type type)
        {
            return type.FullName + ", " + type.Assembly.GetName().Name;
        }
    }
}