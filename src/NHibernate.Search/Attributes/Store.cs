using System;

namespace NHibernate.Search.Attributes
{
    public enum Store
    {
        Yes,
        No,
        [Obsolete("Please use Yes")]
        Compress
    }
}