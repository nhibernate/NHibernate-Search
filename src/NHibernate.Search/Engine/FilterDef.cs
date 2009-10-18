using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace NHibernate.Search.Engine
{
    public delegate void Method();

    public class FilterDef
    {
        private System.Type impl;
        private MethodInfo factoryMethod;
        private MethodInfo keyMethod;
        private Dictionary<string, PropertyInfo> setters;
        private bool cache;

        public FilterDef()
        {
            setters = new Dictionary<string, PropertyInfo>();
        }

        #region Property methods

        public MethodInfo KeyMethod
        {
            get { return keyMethod; }
            set { keyMethod = value; }
        }

        public MethodInfo FactoryMethod
        {
            get { return factoryMethod; }
            set { factoryMethod = value; }
        }

        public System.Type Impl
        {
            get { return impl; }
            set { impl = value; }
        }

        public bool Cache
        {
            get { return cache; }
            set { cache = value; }
        }

        public string Name { get; set; }

        #endregion

        #region Public methods

        public void Invoke(string parameterName, object filter, object parameterValue)
        {
            if (!setters.ContainsKey(parameterName))
            {
                throw new NotSupportedException(
                    string.Format(CultureInfo.InvariantCulture, "No property {0} found in {1}", parameterName,
                                  impl != null ? impl.Name : "<impl>"));
            }

            setters[parameterName].SetValue(filter, parameterValue, null);
        }

        public void AddSetter(PropertyInfo prop)
        {
            setters[prop.Name] = prop;
        }

        #endregion
    }
}