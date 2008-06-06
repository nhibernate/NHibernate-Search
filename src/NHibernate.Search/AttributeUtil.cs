using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using log4net;
using NHibernate.Search.Attributes;

namespace NHibernate.Search
{
    public class AttributeUtil
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(AttributeUtil));

        #region Private methods

        private static T GetAttribute<T>(ICustomAttributeProvider member)
            where T : Attribute
        {
            object[] objects = member.GetCustomAttributes(typeof(T), true);
            if (objects.Length == 0)
                return null;
            return (T) objects[0];
        }

        private static List<T> GetAttributes<T>(ICustomAttributeProvider member)
        {
            object[] objects = member.GetCustomAttributes(typeof(T), true);
            if (objects.Length == 0)
                return null;
            List<T> attribs = new List<T>();
            foreach (T attrib in objects)
                attribs.Add(attrib);

            return attribs;
        }

#endregion

        #region Public methods

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

        public static BoostAttribute GetBoost(MemberInfo member)
        {
            return GetAttribute<BoostAttribute>(member);
        }

        public static List<ClassBridgeAttribute> GetClassBridges(ICustomAttributeProvider member)
        {
            object[] objects = member.GetCustomAttributes(typeof(ClassBridgeAttribute), true);
            if (objects.Length == 0)
                return null;
            List<ClassBridgeAttribute> parameters = new List<ClassBridgeAttribute>();
            foreach (ClassBridgeAttribute parameter in objects)
                parameters.Add(parameter);

            return parameters;
        }

        public static void GetClassBridgeParameters(ICustomAttributeProvider member, List<ClassBridgeAttribute> classBridges)
        {
            // Are we expecting any unnamed parameters?
            bool fieldBridgeExists = GetFieldBridge(member) != null;

            // This is a bit inefficient, but the loops will be very small
            List<ParameterAttribute> parameters = GetParameters(member);

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
                            // Case 2
                            classBridges[0].Parameters.Add(parameter.Name, parameter.Owner);
                        else
                            // Case 3
                            LogParameterError("Parameter needs a name when multiple bridges defined: {0}={1}, parameter={2}", member, parameter);
                    }
                }
                else
                {
                    bool found = false;
                    // Now see if we can find the owner
                    foreach (ClassBridgeAttribute classBridge in classBridges)
                    {
                        if (classBridge.Name == parameter.Name)
                        {
                            classBridge.Parameters.Add(parameter.Name, parameter.Owner);
                            found = true;
                            break;
                        }
                    }

                    // Ok, did we find the appropriate class bridge?
                    if (found == false)
                        LogParameterError("No matching owner for parameter: {0}={1}, parameter={2}, owner={3}", member, parameter);
                }
            }
        }

        public static DateBridgeAttribute GetDateBridge(MemberInfo member)
        {
            return GetAttribute<DateBridgeAttribute>(member);
        }

        public static DocumentIdAttribute GetDocumentId(MemberInfo member)
        {
            DocumentIdAttribute attribute = GetAttribute<DocumentIdAttribute>(member);
            if (attribute == null)
                return null;

            attribute.Name = attribute.Name ?? member.Name;
            return attribute;
        }

        public static FieldAttribute GetField(MemberInfo member)
        {
            FieldAttribute attribute = GetAttribute<FieldAttribute>(member);
            if (attribute == null)
                return null;

            attribute.Name = attribute.Name ?? member.Name;
            return attribute;
        }

        public static List<FieldAttribute> GetFields(MemberInfo member)
        {
            return GetAttributes<FieldAttribute>(member);
        }

        public static FieldBridgeAttribute GetFieldBridge(ICustomAttributeProvider member)
        {
            FieldBridgeAttribute fieldBridge = GetAttribute<FieldBridgeAttribute>(member);
            if (fieldBridge == null)
                return null;

            bool classBridges = GetClassBridges(member) != null;

            // Ok, get all the parameters
            List<ParameterAttribute> parameters = GetParameters(member);
            if (parameters != null)
            {
                foreach (ParameterAttribute parameter in parameters)
                {
                    // Ok, it's ours if there are no class bridges or no owner for the parameter
                    if (!classBridges || string.IsNullOrEmpty(parameter.Owner))
                        fieldBridge.Parameters.Add(parameter.Name, parameter.Value);
                }
            }

            return fieldBridge;
        }

        public static IndexedAttribute GetIndexed(System.Type type)
        {
            return GetAttribute<IndexedAttribute>(type);
        }

        public static List<ParameterAttribute> GetParameters(ICustomAttributeProvider member)
        {
            return GetAttributes<ParameterAttribute>(member);
        }

        public static bool IsIndexed(System.Type type)
        {
            return GetIndexed(type) != null;
        }

        public static bool IsDateBridge(MemberInfo member)
        {
            return GetDateBridge(member) != null;
        }

        #endregion
    }
}