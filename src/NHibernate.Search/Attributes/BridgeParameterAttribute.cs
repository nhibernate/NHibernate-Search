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
        private readonly object value;
        private string owner;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        /// <remarks>Parameter names are capitalized so they appear nice in Intellisense</remarks>
        public ParameterAttribute(string Name, object Value)
        {
            this.name = Name;
            this.value = Value;
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
        public object Value
        {
            get { return value; }
        }

        /// <summary>
        /// The bridge that owns this parameter
        /// </summary>
        public string Owner
        {
            get { return owner; }
            set { owner = value; }
        }
    }
}