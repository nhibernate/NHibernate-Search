using System;
using System.Collections.Generic;
using NHibernate.Search.Attributes;
using Index = NHibernate.Search.Attributes.Index;

namespace NHibernate.Search.Mapping.Definition
{
    using Type = System.Type;

    public interface IClassBridgeDefinition
    {
        string Name                           { get; }
        Attributes.Store Store                { get; }
        Index Index                           { get; }
        Type Analyzer                         { get; }
        float Boost                           { get; }
        Type Impl                             { get; }
        Dictionary<string, object> Parameters { get; }
    }
}
