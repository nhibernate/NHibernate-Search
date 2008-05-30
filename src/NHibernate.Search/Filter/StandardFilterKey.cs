using System;
using System.Collections;

namespace NHibernate.Search.Filter
{
    /// <summary>
    /// Implements a filter key using all injected parameters to compute
    /// equals and hashCode
    /// the order the parameters are added is significant
    /// </summary>
    public class StandardFilterKey : FilterKey
    {
        private readonly ArrayList parameters = new ArrayList();
        private bool implSet;

        #region Property methods

        public override System.Type Impl
        {
            get { return base.Impl; }
            set
            {
                base.Impl = value;
                // Add impl once and once only
                if (implSet)
                    parameters[0] = value;
                else
                {
                    implSet = true;
                    parameters.Insert(0, value);
                }
            }
        }

        #endregion

        #region Public methods

        public void AddParameter(object value)
        {
            parameters.Add(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 23;
            foreach (object param in parameters)
                hash = 31*hash + (param != null ? param.GetHashCode() : 0);

            return hash;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (! (obj is StandardFilterKey)) return false;

            StandardFilterKey that = (StandardFilterKey) obj;

            throw new NotImplementedException("Must override Equals");
        }

        #endregion
    }
}