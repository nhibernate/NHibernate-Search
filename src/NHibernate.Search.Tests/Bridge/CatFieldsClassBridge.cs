using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using NHibernate.Search.Bridge;

namespace NHibernate.Search.Tests.Bridge
{
    public class CatFieldsClassBridge : IFieldBridge, IParameterizedBridge
    {
        private string sepChar;

        #region IFieldBridge Members

        public void Set(string name, object value, Document document, Field.Store store)
        {
            // In this particular class the name of the new field was passed
            // from the name field of the ClassBridge Annotation. This is not
            // a requirement. It just works that way in this instance. The
            // actual name could be supplied by hard coding it below.
            throw new NotImplementedException();
            //Department dep = (Department) value;
            //String fieldValue1 = dep.Branch ?? string.Empty;
            //String fieldValue2 = dep.Network ?? string.Empty;
            //String fieldValue = fieldValue1 + sepChar + fieldValue2;
            //Field field = new Field(name, fieldValue, store, index);
            //if (boost != null)
            //{
            //    field.Boost = (float)boost;
            //}

            //document.Add(field);
        }

        #endregion

        #region IParameterizedBridge Members

        public void SetParameterValues(Dictionary<string, object> parameters)
        {
            sepChar = (string) parameters["sepChar"];
        }

        #endregion
    }
}