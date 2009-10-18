using System;

using NHibernate.Search.Attributes;

namespace NHibernate.Search.Mapping.Definition {
    public interface IDateBridgeDefinition {
        Resolution Resolution { get; }
    }
}
