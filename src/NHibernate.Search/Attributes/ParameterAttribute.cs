using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Parameter (basically key/value pattern)
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class ParameterAttribute : Attribute
    {
        private readonly string name;
        private readonly string value;

        public ParameterAttribute(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Value
        {
            get { return value; }
        }
    }
}