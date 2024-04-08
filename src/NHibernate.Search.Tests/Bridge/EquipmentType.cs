using System.Collections.Generic;
using Lucene.Net.Documents;
using NHibernate.Search.Bridge;

namespace NHibernate.Search.Tests.Bridge
{
    public class EquipmentType : IFieldBridge, IParameterizedBridge
    {
        private Dictionary<string, object> equips;

        #region IFieldBridge Members

        public void Set(string name, object value, Document document, FieldType fieldType, float? boost)
        {
            // In this particular class the name of the new field was passed
            // from the name field of the ClassBridge Annotation. This is not
            // a requirement. It just works that way in this instance. The
            // actual name could be supplied by hard coding it below.
            Departments deps = (Departments)value;
            Field field = null;
            string fieldValue1 = deps.Manufacturer;

            if (fieldValue1 == null)
                fieldValue1 = string.Empty;
            else
            {
                string fieldValue = (string)equips[fieldValue1];
                field = new Field(name, fieldValue, fieldType);
                if (boost != null)
                    field.Boost = (float)boost;
            }

            document.Add(field);
        }

        #endregion

        #region IParameterizedBridge Members

        public void SetParameterValues(Dictionary<string, object> parameters)
        {
            // This map was defined by the parameters of the ClassBridge annotation.
            equips = parameters;
        }

        #endregion
    }
}