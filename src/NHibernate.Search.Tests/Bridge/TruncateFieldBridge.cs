using System;
using Lucene.Net.Documents;
using NHibernate.Search.Bridge;
using NHibernate.Util;

namespace NHibernate.Search.Tests.Bridge
{
    public class TruncateFieldBridge : IFieldBridge
    {
        #region IFieldBridge Members

        public void Set(string name, object value, Document document, FieldType fieldType, float? boost)
        {
            String indexedString = (String)value;
            //Do not add fields on empty strings, seems a sensible default in most situations
            if (StringHelper.IsNotEmpty(indexedString))
            {
                Field field = new Field(name, indexedString.Substring(0, indexedString.Length / 2), fieldType);

                if (boost != null) field.Boost = boost.Value;
                document.Add(field);
            }
        }

        #endregion

        public Object Get(String name, Document document)
        {
            var field = document.GetField(name);
            return field.GetStringValue();
        }
    }
}