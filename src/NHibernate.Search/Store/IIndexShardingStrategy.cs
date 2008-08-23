using System;
using System.Collections.Generic;
using System.Text;

namespace NHibernate.Search.Store
{
    /// <summary>
    /// Defines how a given virtual index shards data into different <see cref="IDirectoryProvider" />s
    /// </summary>
    public interface IIndexShardingStrategy
    {
        /// <summary>
        /// Provides access to sharding properties (under the suffix sharding_strategy)
        /// and provide access to all the <see cref="IDirectoryProvider" />s for a given index.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="providers"></param>
        void Initialize(object properties, IDirectoryProvider[] providers);

        /// <summary>
        /// Ask for all shards (eg to query or optimize)
        /// </summary>
        /// <returns></returns>
        IDirectoryProvider[] GetDirectoryProvidersForAllShards();

        /// <summary>
        /// Return the <see cref="IDirectoryProvider"/> where the given entity will be indexed
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <param name="idInString"></param>
        /// <param name="document"></param>
        /// <returns></returns>
        IDirectoryProvider GetDirectoryProviderForAddition(System.Type entity, object id, string idInString, Lucene.Net.Documents.Document document);

        /// <summary>
        /// Return the <see cref="IDirectoryProvider"/>(s) where the given entity is stored and where the deletion operation needs to be applied
        /// id and idInString can be null. If null, all the directory providers containing entity types should be returned
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <param name="idInString"></param>
        /// <returns></returns>
        IDirectoryProvider[] GetDirectoryProvidersForDeletion(System.Type entity, object id, string idInString);
    }
}
