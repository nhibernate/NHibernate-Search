using System;
using System.Collections.Generic;
using NHibernate.Search.Attributes;
using NHibernate.Search.Bridge.Builtin;
using NHibernate.Search.Mapping.Definition;

namespace NHibernate.Search.Bridge
{
    public static class BridgeFactory
    {
        public static readonly ITwoWayFieldBridge BOOLEAN = new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<bool>());
        public static readonly IFieldBridge DATE_DAY = new String2FieldBridgeAdaptor(DateBridge.DATE_DAY);
        public static readonly IFieldBridge DATE_HOUR = new String2FieldBridgeAdaptor(DateBridge.DATE_HOUR);
        public static readonly ITwoWayFieldBridge DATE_MILLISECOND = new TwoWayString2FieldBridgeAdaptor(DateBridge.DATE_MILLISECOND);
        public static readonly IFieldBridge DATE_MINUTE = new String2FieldBridgeAdaptor(DateBridge.DATE_MINUTE);
        public static readonly IFieldBridge DATE_MONTH = new String2FieldBridgeAdaptor(DateBridge.DATE_MONTH);
        public static readonly IFieldBridge DATE_SECOND = new String2FieldBridgeAdaptor(DateBridge.DATE_SECOND);
        public static readonly IFieldBridge DATE_YEAR = new String2FieldBridgeAdaptor(DateBridge.DATE_YEAR);
        public static readonly ITwoWayFieldBridge DOUBLE = new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<double>());
        public static readonly ITwoWayFieldBridge FLOAT = new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<float>());
        public static readonly ITwoWayFieldBridge INTEGER = new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<int>());
        public static readonly ITwoWayFieldBridge LONG = new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<long>());
        public static readonly ITwoWayFieldBridge SHORT = new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<short>());
        public static readonly ITwoWayFieldBridge STRING = new TwoWayString2FieldBridgeAdaptor(new StringBridge());
        public static readonly ITwoWayFieldBridge GUID = new TwoWayString2FieldBridgeAdaptor(new GuidBridge());

        private static readonly Dictionary<string, IFieldBridge> builtInBridges = new Dictionary<string, IFieldBridge>();

        static BridgeFactory()
        {
            builtInBridges.Add(typeof(double).Name, DOUBLE);
            builtInBridges.Add(typeof(float).Name, FLOAT);
            builtInBridges.Add(typeof(short).Name, SHORT);
            builtInBridges.Add(typeof(int).Name, INTEGER);
            builtInBridges.Add(typeof(long).Name, LONG);
            builtInBridges.Add(typeof(String).Name, STRING);
            builtInBridges.Add(typeof(Boolean).Name, BOOLEAN);
            builtInBridges.Add(typeof(Guid).Name, GUID);

            builtInBridges.Add(typeof(DateTime).Name, DATE_MILLISECOND);
        }

        public static IFieldBridge ExtractType(IClassBridgeDefinition cb)
        {
            IFieldBridge bridge = null;

            if (cb != null)
            {
                System.Type impl = cb.Impl;

                if (impl != null)
                {
                    try
                    {
                        object instance = Activator.CreateInstance(impl);
                        if (instance is IFieldBridge)
                        {
                            bridge = (IFieldBridge) instance;
                        }

                        if (cb.Parameters.Count > 0 && instance is IParameterizedBridge)
                        {
                            // Already converted the parameters by this stage
                            ((IParameterizedBridge) instance).SetParameterValues(cb.Parameters);
                        }
                    }
                    catch (Exception e)
                    {
                        // TODO add classname
                        throw new HibernateException("Unable to instantiate IFieldBridge for " + cb.Name, e);
                    }
                }
            }
            // TODO add classname
            if (bridge == null)
            {
                throw new HibernateException("Unable to guess IFieldBridge ");
            }

            return bridge;
        }

        public static IFieldBridge GuessType(
            string fieldName, 
            System.Type fieldType,
            IFieldBridgeDefinition fieldBridgeDefinition,
            IDateBridgeDefinition dateBridgeDefinition
        )
        {
            IFieldBridge bridge = null;
            if (fieldBridgeDefinition != null)
            {
                System.Type impl = fieldBridgeDefinition.Impl;
                try
                {
                    object instance = Activator.CreateInstance(impl);
                    if (instance is IFieldBridge)
                    {
                        bridge = (IFieldBridge) instance;
                    }
                    else if (instance is ITwoWayStringBridge)
                    {
                        bridge = new TwoWayString2FieldBridgeAdaptor((ITwoWayStringBridge) instance);
                    }
                    else if (instance is IStringBridge)
                    {
                        bridge = new String2FieldBridgeAdaptor((IStringBridge) instance);
                    }

                    if (fieldBridgeDefinition.Parameters.Count > 0 && instance is IParameterizedBridge)
                    {
                        ((IParameterizedBridge) instance).SetParameterValues(fieldBridgeDefinition.Parameters);
                    }
                }
                catch (Exception e)
                {
                    // TODO add classname
                    throw new HibernateException("Unable to instantiate IFieldBridge for " + fieldName, e);
                }
            }
            else if (dateBridgeDefinition != null)
            {
                bridge = GetDateField(dateBridgeDefinition.Resolution);
            }
            else
            {
                // find in built-ins
                System.Type returnType = fieldType;
                if (IsNullable(returnType))
                {
                    returnType = returnType.GetGenericArguments()[0];
                }

                builtInBridges.TryGetValue(returnType.Name, out bridge);
                if (bridge == null && returnType.IsEnum)
                {
                    bridge = new TwoWayString2FieldBridgeAdaptor(new EnumBridge(returnType));
                }
            }

            // TODO add classname
            if (bridge == null)
            {
                throw new HibernateException("Unable to guess IFieldBridge for " + fieldName);
            }

            return bridge;
        }

        public static IFieldBridge GetDateField(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Year:
                    return DATE_YEAR;
                case Resolution.Month:
                    return DATE_MONTH;
                case Resolution.Day:
                    return DATE_DAY;
                case Resolution.Hour:
                    return DATE_HOUR;
                case Resolution.Minute:
                    return DATE_MINUTE;
                case Resolution.Second:
                    return DATE_SECOND;
                case Resolution.Millisecond:
                    return DATE_MILLISECOND;
                default:
                    throw new AssertionFailure("Unknown Resolution: " + resolution);
            }
        }

        private static bool IsNullable(System.Type returnType)
        {
            return returnType.IsGenericType && typeof(Nullable<>) == returnType.GetGenericTypeDefinition();
        }
    }
}