namespace NHibernate.Search.Bridge
{
    /// <summary>
    /// IStringBridge allowing a translation from the string back to the object
    /// ObjectToString( StringToObject( string ) ) and StringToObject( objectToString( object ) )
    /// should be "idempotent". More precisely,
    /// 
    /// ObjectToString( stringToObject( string ) ).Equals(string) for string not null
    /// StringToObject( objectToString( object ) ).Equals(object) for object not null 
    /// </summary>
    public interface ITwoWayStringBridge : IStringBridge
    {
        /// <summary>
        /// Convert the string representation to an object
        /// </summary>
        object StringToObject(string stringValue);
    }
}