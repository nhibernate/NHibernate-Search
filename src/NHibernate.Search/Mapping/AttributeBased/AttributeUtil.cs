using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using NHibernate.Search.Attributes;

namespace NHibernate.Search.Mapping.AttributeBased
{
    public class AttributeUtil
    {
		private static readonly IInternalLogger logger = LoggerProvider.LoggerFor(typeof(AttributeUtil));

        #region Public methods

        public static T GetAttribute<T>(ICustomAttributeProvider member) where T : Attribute
        {
            object[] objects = member.GetCustomAttributes(typeof(T), true);
            if (objects.Length == 0)
            {
                return null;
            }

            return (T) objects[0];
        }

        public static bool HasAttribute<T>(ICustomAttributeProvider member) where T : Attribute
        {
            return member.IsDefined(typeof(T), true);
        }

        public static T[] GetAttributes<T>(ICustomAttributeProvider member)
            where T : class
        {
            return GetAttributes<T>(member, true);
        }

        public static T[] GetAttributes<T>(ICustomAttributeProvider member, bool inherit)
            where T : class
        {
            return (T[])member.GetCustomAttributes(typeof(T), inherit);
        }

        public static void GetClassBridgeParameters(ICustomAttributeProvider member, IList<ClassBridgeAttribute> classBridges)
        {
            // Are we expecting any unnamed parameters?
            bool fieldBridgeExists = GetFieldBridge(member) != null;

            // This is a bit inefficient, but the loops will be very small
            IList<ParameterAttribute> parameters = GetParameters(member);

            // Do it this way around so we can ensure we process all parameters
            foreach (ParameterAttribute parameter in parameters)
            {
                // What we want to ensure is :
                // 1. If there's a field bridge, unnamed parameters belong to it
                // 2. If there's 1 class bridge and no field bridge, unnamed parameters belong to it
                // 3. If there's > 1 class bridge and no field bridge, that's an error - we don't know which class bridge should get them
                if (string.IsNullOrEmpty(parameter.Owner))
                {
                    if (!fieldBridgeExists)
                    {
                        if (classBridges.Count == 1)
                        {
                            // Case 2
                            classBridges[0].Parameters.Add(parameter.Name, parameter.Value);
                        }
                        else
                        {
                            // Case 3
                            LogParameterError(
                                    "Parameter needs a name when multiple bridges defined: {0}={1}, parameter={2}",
                                    member,
                                    parameter);
                        }
                    }
                }
                else
                {
                    bool found = false;

                    // Now see if we can find the owner
                    foreach (ClassBridgeAttribute classBridge in classBridges)
                    {
                        if (classBridge.Name == parameter.Owner)
                        {
                            classBridge.Parameters.Add(parameter.Name, parameter.Value);
                            found = true;
                            break;
                        }
                    }

                    // Ok, did we find the appropriate class bridge?
                    if (found == false)
                    {
                        LogParameterError(
                                "No matching owner for parameter: {0}={1}, parameter={2}, owner={3}", member, parameter);
                    }
                }
            }
        }

        public static FieldAttribute GetField(MemberInfo member)
        {
            FieldAttribute attribute = GetAttribute<FieldAttribute>(member);
            if (attribute == null)
            {
                return null;
            }

            attribute.Name = attribute.Name ?? member.Name;
            return attribute;
        }

        public static FieldAttribute[] GetFields(MemberInfo member)
        {
            FieldAttribute[] attribs = GetAttributes<FieldAttribute>(member);
            if (attribs != null)
            {
                foreach (FieldAttribute attribute in attribs)
                {
                    attribute.Name = attribute.Name ?? member.Name;
                }
            }

            return attribs;
        }

        public static FieldBridgeAttribute GetFieldBridge(ICustomAttributeProvider member)
        {
            FieldBridgeAttribute fieldBridge = GetAttribute<FieldBridgeAttribute>(member);
            if (fieldBridge == null)
            {
                return null;
            }

            bool classBridges = GetAttributes<ClassBridgeAttribute>(member) != null;

            // Ok, get all the parameters
            IList<ParameterAttribute> parameters = GetParameters(member);
            if (parameters != null)
            {
                foreach (ParameterAttribute parameter in parameters)
                {
                    // Ok, it's ours if there are no class bridges or no owner for the parameter
                    if (!classBridges || string.IsNullOrEmpty(parameter.Owner))
                    {
                        fieldBridge.Parameters.Add(parameter.Name, parameter.Value);
                    }
                }
            }

            return fieldBridge;
        }

        public static IList<ParameterAttribute> GetParameters(ICustomAttributeProvider member)
        {
            return GetAttributes<ParameterAttribute>(member);
        }

        #endregion

        #region Private methods

        private static void LogParameterError(string message, ICustomAttributeProvider member, ParameterAttribute parameter)
        {
            string type = string.Empty;
            string name = string.Empty;

            if (typeof(System.Type).IsAssignableFrom(member.GetType()))
            {
                type = "class";
                name = ((System.Type)member).FullName;
            }
            else if (typeof(MemberInfo).IsAssignableFrom(member.GetType()))
            {
                type = "member";
                name = ((MemberInfo)member).DeclaringType.FullName + "." + ((MemberInfo)member).DeclaringType.FullName;
            }

            // Now log it
            logger.Error(string.Format(CultureInfo.InvariantCulture, message, type, name, parameter.Name, parameter.Owner));
        }

        #endregion
    }
}
