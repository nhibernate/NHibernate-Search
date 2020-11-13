using System;
using Lucene.Net.Documents;

namespace NHibernate.Search.Bridge
{
    /// <summary>
    /// Bridge to use a TwoWayStringBridge as a TwoWayFieldBridge
    /// </summary>
    public class TwoWayString2FieldBridgeAdaptor : String2FieldBridgeAdaptor, ITwoWayFieldBridge
    {
        private readonly ITwoWayStringBridge stringBridge;

        public TwoWayString2FieldBridgeAdaptor(ITwoWayStringBridge stringBridge) : base(stringBridge)
        {
            this.stringBridge = stringBridge;
        }

        #region ITwoWayFieldBridge Members

        public object Get(String name, Document document)
        {
            var field = document.GetField(name);
            return field == null ? null : stringBridge.StringToObject(field.GetStringValue());
        }

        public string ObjectToString(object obj)
        {
            return stringBridge.ObjectToString(obj);
        }

        #endregion
    }
}