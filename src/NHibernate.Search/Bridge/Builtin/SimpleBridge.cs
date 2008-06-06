namespace NHibernate.Search.Bridge.Builtin
{
    public abstract class SimpleBridge : ITwoWayStringBridge
    {
        #region ITwoWayStringBridge Members

        public abstract object StringToObject(string stringValue);

        public virtual string ObjectToString(object obj)
        {
            return obj == null ? null : obj.ToString();
        }

        #endregion
    }
}