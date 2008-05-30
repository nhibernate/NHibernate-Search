using System;
using System.Collections.Generic;
using System.Globalization;

namespace NHibernate.Search.Engine
{
    public delegate void Method();

    public class FilterDef
    {
        private System.Type impl;
        private Method factoryMethod;
        private Method keyMethod;
        private Dictionary<string, Method> setters;
        private bool isCache;

        public FilterDef()
        {
            setters = new Dictionary<string, Method>();
        }

        #region Property methods

        /// <summary>
        /// 
        /// </summary>
        public Method KeyMethod
        {
            get { return keyMethod; }
            set { keyMethod = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Method FactoryMethod
        {
            get { return factoryMethod; }
            set { factoryMethod = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public System.Type Impl
        {
            get { return impl; }
            set { impl = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsCache
        {
            get { return isCache; }
            set { isCache = value; }
        }

        #endregion

        #region Public methods

        public void Invoke(string parameterName, object filter, object parameterValue)
        {
            Method method = setters[parameterName];
            if (method == null)
                throw new NotSupportedException(
                    string.Format(CultureInfo.InvariantCulture, "No setter {0} found in {1}", parameterName,
                                  impl != null ? impl.Name : "<impl>"));

            throw new NotImplementedException("Method not implemented");
            //method.Invoke(filter, parameterValue);
        }

        public void AddSetter(string name, Method method)
        {
            throw new NotImplementedException("Method not implemented");
        }

        #endregion
    }
}