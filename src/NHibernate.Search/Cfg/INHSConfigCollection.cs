using System;
using System.Collections.Generic;
using System.Text;

namespace NHibernate.Search.Cfg
{
  /// <summary>
  /// INHSConfigCollection contains a dictionary of instantiated <see cref="INHSConfiguration" /> 
  /// objects. The API expects the key to be the NHibernate session factory name. 
  /// </summary>
  /// <remarks>
  /// The default concrete implementation is <see cref="NHSConfigCollection" />.
  /// </remarks>
  public interface INHSConfigCollection : IDictionary<string, INHSConfiguration>
  {
    /// <summary>
    /// As a convenience, we will treat an INHSConfiguration with an empty key 
    /// as the default confguration.
    /// </summary>
    bool HasDefaultConfiguration { get; }

    /// <summary>
    /// If collection has a default configuration, then return that instance.
    /// Otherwise, return null.
    /// </summary>
    INHSConfiguration DefaultConfiguration { get; }

    /// <summary>
    /// Gets the <see cref="INHSConfiguration" /> for the specified key.
    /// </summary>
    /// <param name="sessionFactoryName">The NHibernate session factory name.</param>
    /// <returns>
    /// If collection has an instance of <see cref="INHSConfiguration" /> for the named session factory, then return that instance.
    /// Otherwise, if collection has a default configuration, then return that instance.
    /// Otherwise, return null.
    /// </returns>
    INHSConfiguration GetConfiguration(string sessionFactoryName);
  }
}
