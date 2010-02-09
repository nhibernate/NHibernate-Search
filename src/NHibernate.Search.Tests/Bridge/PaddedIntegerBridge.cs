namespace NHibernate.Search.Tests.Bridge
{
    using System;
    using System.Collections.Generic;

    using NHibernate.Search.Bridge;

    public class PaddedIntegerBridge : ITwoWayStringBridge, IParameterizedBridge
    {
        public static string PADDING_PROPERTY = "padding";

        private int padding = 5; // default;

        public void SetParameterValues(Dictionary<string, object> parameters)
        {
            object value;
            if (parameters.TryGetValue(PADDING_PROPERTY, out value))
            {
                this.padding = (int) value;
            }
        }

        public string ObjectToString(object obj)
        {
            int value = (int) obj;
            string rawInteger = value.ToString();
            if (rawInteger.Length > padding)
            {
                throw new ArgumentOutOfRangeException("obj", "Try to pad on a number too big");
            }

            return value.ToString("D" + padding);
        }

        public object StringToObject(string value)
        {
            return int.Parse(value);
        }
    }
}
