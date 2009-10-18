using System;
using NHibernate.Search.Mapping.Definition;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Defines the temporal resolution of a given field
    /// Date are stored as String in GMT
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DateBridgeAttribute : Attribute, IDateBridgeDefinition
    {
        private readonly Resolution resolution;

        public DateBridgeAttribute(Resolution resolution)
        {
            this.resolution = resolution;
        }

        public Resolution Resolution
        {
            get { return resolution; }
        }
    }
}