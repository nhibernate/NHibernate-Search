using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Marks a method as a factory method for a given type.
    /// A factory method is called whenever a new instance of a given
    /// type is requested.
    /// The factory method is used with a higher priority than a plain no-arg constructor when present
    ///
    /// Factory currently works for classes supplied to <see cref="FullTextFilterDefAttribute.Impl" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactoryAttribute : Attribute
    {
    }
}