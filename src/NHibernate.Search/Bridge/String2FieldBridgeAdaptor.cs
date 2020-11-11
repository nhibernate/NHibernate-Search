using System;
using Lucene.Net.Documents;
using NHibernate.Util;

namespace NHibernate.Search.Bridge
{
    /// <summary>
    /// Bridge to use a StringBridge as a FieldBridge
    /// </summary>
    public class String2FieldBridgeAdaptor : IFieldBridge
    {
        private readonly IStringBridge stringBridge;

        public String2FieldBridgeAdaptor(IStringBridge stringBridge)
        {
            this.stringBridge = stringBridge;
        }

        #region IFieldBridge Members

        public void Set(String name, Object value, Document document, Field.Store store)
        {
            string indexedString = stringBridge.ObjectToString(value);

            // Do not add fields on empty strings, seems a sensible default in most situations
            // TODO if Store, probably also save empty ones
            if (StringHelper.IsNotEmpty(indexedString))
            {
                var field = new StringField(name, indexedString, store);

                document.Add(field);
            }
        }

        #endregion
    }
}