using System.Collections;
using Lucene.Net.Store;
using NHibernate.Search.Impl;

namespace NHibernate.Search.Store
{
    public interface IDirectoryProvider
    {
        Directory Directory { get; }
        void Initialize(string directoryProviderName, IDictionary indexProps, SearchFactoryImpl searchFactory);
    }
}