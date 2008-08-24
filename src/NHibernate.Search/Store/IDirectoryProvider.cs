using System.Collections.Generic;
using Lucene.Net.Store;
using NHibernate.Search.Engine;

namespace NHibernate.Search.Store
{
    public interface IDirectoryProvider
    {
        /// <summary>
        /// Returns an initialized Lucene Directory. This method call <b>must</b> be threadsafe
        /// </summary>
        Directory Directory { get; }

        /// <summary>
        /// get the information to initialize the directory and build its hashCode/equals method
        /// </summary>
        /// <param name="directoryProviderName"></param>
        /// <param name="indexProps"></param>
        /// <param name="searchFactory"></param>
        void Initialize(string directoryProviderName, IDictionary<string, string> indexProps, ISearchFactoryImplementor searchFactory);

        /// <summary>
        /// Executed after initialize, this method set up the heavy process of starting up the DirectoryProvider
        /// IO processing as well as backgroup processing are expected to be set up here
        /// </summary>
        /// TODO stop() method, for now use finalize() 
        void Start();
    }
}