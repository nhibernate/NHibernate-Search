using System;
using Lucene.Net.Documents;
using NHibernate.Search.Bridge;
using NHibernate.Util;

namespace NHibernate.Search.Tests.Bridge
{
    public class TruncateFieldBridge : IFieldBridge
    {
        #region IFieldBridge Members

        public void Set(String name, Object value, Document document, Field.Store store, Field.Index index, float? boost)
        {
            String indexedString = (String) value;
            //Do not add fields on empty strings, seems a sensible default in most situations
            if (StringHelper.IsNotEmpty(indexedString))
            {
                Field field = new Field(name, indexedString.Substring(0, indexedString.Length/2), store, index);
                if (boost != null) field.SetBoost(boost.Value);
                document.Add(field);
            }
        }

        #endregion

        public Object Get(String name, Document document)
        {
            Field field = document.GetField(name);
            return field.StringValue();
        }
    }
}