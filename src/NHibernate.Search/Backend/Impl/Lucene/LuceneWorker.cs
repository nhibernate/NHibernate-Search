using System;
using System.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Search.Store;
using NHibernate.Search.Util;

namespace NHibernate.Search.Backend.Impl.Lucene
{
    public class LuceneWorker
    {
		private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(LuceneWorker));
        private readonly Workspace _workspace;

        public LuceneWorker(Workspace workspace)
        {
            _workspace = workspace;
        }

        public void PerformWork(WorkWithPayload luceneWork)
        {
            switch (luceneWork.Work)
            {
                case AddLuceneWork addWork:
                    PerformWork(addWork, luceneWork.Provider);
                    break;
                case DeleteLuceneWork deleteWork:
                    PerformWork(deleteWork, luceneWork.Provider);
                    break;
                case OptimizeLuceneWork optimizeWork:
                    PerformWork(optimizeWork, luceneWork.Provider);
                    break;
                case PurgeAllLuceneWork purgeWork:
                    PerformWork(purgeWork, luceneWork.Provider);
                    break;
                default:
                    throw new AssertionFailure("Unknown work type: " + luceneWork.GetType());
            }
        }

        public void PerformWork(AddLuceneWork work, IDirectoryProvider provider)
        {
            Add(work.EntityClass, work.Id, work.Document, provider);
        }

        public void PerformWork(DeleteLuceneWork work, IDirectoryProvider provider)
        {
            Remove(work.EntityClass, work.Id, provider);
        }

        public void PerformWork(OptimizeLuceneWork work, IDirectoryProvider provider)
        {
            var entity = work.EntityClass;
            if (log.IsDebugEnabled)
            {
                log.Debug("Optimize Lucene index: " + entity);
            }
            var writer = _workspace.GetIndexWriter(provider, entity, false);

            try
            {
                writer.ForceMerge(1);
                _workspace.Optimize(provider);
            }
            catch (IOException e)
            {
                throw new SearchException("Unable to optimize Lucene index: " + entity, e);
            }
        }

        public void PerformWork(PurgeAllLuceneWork work, IDirectoryProvider provider)
        {
            var entity = work.EntityClass;
            if (log.IsDebugEnabled)
            {
                log.Debug("PurgeAll Lucene index: " + entity);
            }

            var writer = _workspace.GetIndexWriter(provider, entity, true);
            try
            {
                writer.DeleteAll();
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to purge all from Lucene index: " + entity, e);
            }
        }

        private void Add(System.Type entity, object id, Document document, IDirectoryProvider provider)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug("Add to Lucene index: " + entity + "#" + id + ": " + document);
            }

            var writer = _workspace.GetIndexWriter(provider, entity, true);

            try
            {
                writer.AddDocument(document);
            }
            catch (IOException e)
            {
                throw new SearchException("Unable to Add to Lucene index: " + entity + "#" + id, e);
            }
        }

        private void Remove(System.Type entity, object id, IDirectoryProvider provider)
        {
            log.DebugFormat("remove from Lucene index: {0}#{1}", entity, id);
            var builder = _workspace.GetDocumentBuilder(entity);
            var term = builder.GetTerm(id);
            var writer = _workspace.GetIndexWriter(provider, entity, true);
            try
            {
                writer.DeleteDocuments(term);
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to remove from Lucene index: " + entity + "#" + id, e);
            }
        }

        public class WorkWithPayload
        {
            public WorkWithPayload(LuceneWork work, IDirectoryProvider provider)
            {
                Work = work;
                Provider = provider;
            }

            public LuceneWork Work { get; }

            public IDirectoryProvider Provider { get; }
        }

    }
}