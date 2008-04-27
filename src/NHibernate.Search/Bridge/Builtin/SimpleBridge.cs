namespace NHibernate.Search.Bridge.Builtin {
    public abstract class SimpleBridge : ITwoWayStringBridge {
        #region ITwoWayStringBridge Members

        public abstract object StringToObject(string stringValue);

        public string ObjectToString(object obj) {
            if (obj == null)
                return null;
            return obj.ToString();
        }

        #endregion
    }
}