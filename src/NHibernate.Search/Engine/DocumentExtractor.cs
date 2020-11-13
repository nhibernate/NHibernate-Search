using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace NHibernate.Search.Engine
{
    public class DocumentExtractor
    {
        private readonly ISearchFactoryImplementor searchFactoryImplementor;
        private readonly string[] projection;

        public DocumentExtractor(ISearchFactoryImplementor searchFactoryImplementor, string[] projection)
        {
            this.searchFactoryImplementor = searchFactoryImplementor;
            this.projection = projection;
        }

        private EntityInfo Extract(Document document)
        {
            EntityInfo entityInfo = new EntityInfo();
            entityInfo.Clazz = DocumentBuilder.GetDocumentClass(document);
            entityInfo.Id = DocumentBuilder.GetDocumentId(searchFactoryImplementor, entityInfo.Clazz, document);
            if (projection != null && projection.Length > 0)
            {
                entityInfo.Projection = DocumentBuilder.GetDocumentFields(searchFactoryImplementor, entityInfo.Clazz,
                                                                          document, projection);
            }
            return entityInfo;
        }
        public EntityInfo Extract(TopDocs topDocs, Lucene.Net.Search.IndexSearcher searcher, int index)
        {
            ScoreDoc scoreDoc = topDocs.ScoreDocs[index];
            Document doc = searcher.Doc(scoreDoc.Doc);
            //TODO if we are lonly looking for score (unlikely), avoid accessing doc (lazy load)
            EntityInfo entityInfo = Extract(doc);
            object[] eip = entityInfo.Projection;

            if (eip != null && eip.Length > 0)
            {
                for (int x = 0; x<projection.Length; x++)
                {
                    switch (projection[x])
                    {
                        case ProjectionConstants.SCORE:
                            eip[x] = scoreDoc.Score;
                            break;

                        case ProjectionConstants.ID:
                            eip[x] = entityInfo.Id;
                            break;

                        case ProjectionConstants.DOCUMENT:
                            eip[x] = doc;
                            break;

                        case ProjectionConstants.DOCUMENT_ID:
                            eip[x] = scoreDoc.Doc;
                            break;

                        //case ProjectionConstants.BOOST:
                        //    eip[x] = doc.Boost;
                        //    break;

                        case ProjectionConstants.THIS:
                            //THIS could be projected more than once
                            //THIS loading delayed to the Loader phase
                            if (entityInfo.IndexesOfThis == null)
                            {
                                entityInfo.IndexesOfThis = new List<int>(1);
                            }
                            entityInfo.IndexesOfThis.Add(x);
                            break;
                    }
                }
            }

            return entityInfo;
        }
    }
    
}