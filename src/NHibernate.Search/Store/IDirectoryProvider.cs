using System.Collections;
using Lucene.Net.Store;
using NHibernate.Search.Engine;

namespace NHibernate.Search.Storage {
    public interface IDirectoryProvider {
        Directory Directory { get; }
        void Initialize(string directoryProviderName, IDictionary indexProps, SearchFactory searchFactory);
    }
}