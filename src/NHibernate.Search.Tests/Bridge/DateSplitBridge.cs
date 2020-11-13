using System;

namespace NHibernate.Search.Tests.Bridge
{
    using Lucene.Net.Documents;

    using NHibernate.Search.Bridge;

    public class DateSplitBridge : IFieldBridge
    {
        public void Set(string name, object value, Document document, Field.Store store)
        {
            //DateTime date = (DateTime) value;

            //int year = date.Year;
            //int month = date.Month;
            //int day = date.Day;

            //// set year
            //Field field = new Field(name + ".year", year.ToString(), store, index);
            //if (boost != null)
            //{
            //    field.Boost = boost.Value;
            //}
            //document.Add(field);

            //// set month and pad it if necessary
            //field = new Field(name + ".month", month.ToString("D2"), store, index);
            //if (boost != null)
            //{
            //    field.Boost = boost.Value;
            //}
            //document.Add(field);

            //// set day and pad it if necessary
            //field = new Field(name + ".day", day.ToString("D2"), store, index);
            //if (boost != null)
            //{
            //    field.Boost = boost.Value;
            //}
            //document.Add(field);

            throw new NotImplementedException();
        }
    }
}
