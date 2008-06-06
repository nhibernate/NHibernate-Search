using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NHibernate.Search.Util
{
    public abstract class BinderHelper
    {
        /// <summary>
        /// Get the attribute name out of the member unless overridden by name
        /// </summary>
        /// <param name="member"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetAttributeName(MemberInfo member, string name)
        {
            return !string.IsNullOrEmpty(name) ? name : member.Name;
        }
    }
}
