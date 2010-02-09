using System;
using System.IO;
using log4net;
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
        private static readonly ILog log = LogManager.GetLogger(typeof(LuceneWorker));
        private readonly Workspace workspace;

        #region Constructors

        public LuceneWorker(Workspace workspace)
        {
            this.workspace = workspace;
        }

        #endregion

        #region Public methods

        public void PerformWork(WorkWithPayload luceneWork)
        {
            if (luceneWork.Work is AddLuceneWork)
            {
                PerformWork((AddLuceneWork) luceneWork.Work, luceneWork.Provider);
            }
            else if (luceneWork.Work is DeleteLuceneWork)
            {
                PerformWork((DeleteLuceneWork) luceneWork.Work, luceneWork.Provider);
            }
            else if (luceneWork.Work is OptimizeLuceneWork)
            {
                PerformWork((OptimizeLuceneWork) luceneWork.Work, luceneWork.Provider);
            }
            else if (luceneWork.Work is PurgeAllLuceneWork)
            {
                PerformWork((PurgeAllLuceneWork)luceneWork.Work, luceneWork.Provider);
            }
            else
            {
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
            System.Type entity = work.EntityClass;
            if (log.IsDebugEnabled)
            {
                log.Debug("Optimize Lucene index: " + entity);
            }
            IndexWriter writer = workspace.GetIndexWriter(provider, entity, false);

            try
            {
                writer.Optimize();
                workspace.Optimize(provider);
            }
            catch (IOException e)
            {
                throw new SearchException("Unable to optimize Lucene index: " + entity, e);
            }
        }

        public void PerformWork(PurgeAllLuceneWork work, IDirectoryProvider provider)
        {
            System.Type entity = work.EntityClass;
            if (log.IsDebugEnabled)
            {
                log.Debug("PurgeAll Lucene index: " + entity);
            }

            IndexReader reader = workspace.GetIndexReader(provider, entity);
            try
            {
                Term term = new Term(DocumentBuilder.CLASS_FIELDNAME, TypeHelper.LuceneTypeName(entity));
                reader.DeleteDocuments(term);
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to purge all from Lucene index: " + entity, e);
            }
        }

        #endregion

        #region Private methods

        private void Add(System.Type entity, object id, Document document, IDirectoryProvider provider)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug("Add to Lucene index: " + entity + "#" + id + ": " + document);
            }

            IndexWriter writer = workspace.GetIndexWriter(provider, entity, true);

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
            /*
            * even with Lucene 2.1, use of indexWriter to delete is not an option
            * We can only delete by term, and the index doesn't have a termt that
            * uniquely identify the entry. See logic below
            */
            log.DebugFormat("remove from Lucene index: {0}#{1}", entity, id);
            DocumentBuilder builder = workspace.GetDocumentBuilder(entity);
            Term term = builder.GetTerm(id);
            IndexReader reader = workspace.GetIndexReader(provider, entity);
            TermDocs termDocs = null;
            try
            {
                // TODO is there a faster way?
                // TODO include TermDocs into the workspace?
                termDocs = reader.TermDocs(term);
                string entityName = TypeHelper.LuceneTypeName(entity);
                while (termDocs.Next())
                {
                    int docIndex = termDocs.Doc();
                    if (entityName.Equals(reader.Document(docIndex).Get(DocumentBuilder.CLASS_FIELDNAME)))
                    {
                        // remove only the one of the right class
                        // loop all to remove all the matches (defensive code)
                        reader.DeleteDocument(docIndex);
                    }
                }
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to remove from Lucene index: " + entity + "#" + id, e);
            }
            finally
            {
                if (termDocs != null)
                {
                    try
                    {
                        termDocs.Close();
                    }
                    catch (IOException e)
                    {
                        log.Warn("Unable to close termDocs properly", e);
                    }
                }
            }
        }

        #endregion

        #region Nested classes: WorkWithPayload

        public class WorkWithPayload
        {
            private readonly IDirectoryProvider provider;
            private readonly LuceneWork work;

            public WorkWithPayload(LuceneWork work, IDirectoryProvider provider)
            {
                this.work = work;
                this.provider = provider;
            }

            public LuceneWork Work
            {
                get { return work; }
            }

            public IDirectoryProvider Provider
            {
                get { return provider; }
            }
        }

        #endregion
    }
}