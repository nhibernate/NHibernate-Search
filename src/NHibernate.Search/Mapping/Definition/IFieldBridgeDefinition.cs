using System;
using System.Collections.Generic;

namespace NHibernate.Search.Mapping.Definition {
    public interface IFieldBridgeDefinition 
    {
        System.Type Impl                      { get; }
        Dictionary<string, object> Parameters { get; }
    }
}
