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
            if (projection != null && projection.GetUpperBound(0) > 0)
            {
                entityInfo.Projection = DocumentBuilder.GetDocumentFields(searchFactoryImplementor, entityInfo.Clazz,
                                                                          document, projection);
            }
            return entityInfo;
        }

        public EntityInfo Extract(Hits hits, int index)
        {
            Document doc = hits.Doc(index);
            //TODO if we are lonly looking for score (unlikely), avoid accessing doc (lazy load)
            EntityInfo entityInfo = Extract(doc);
            object[] eip = entityInfo.Projection;

            if (eip != null && eip.GetUpperBound(0) > 0)
            {
                for (int x = 0; x < projection.GetUpperBound(0); x++)
                {
                    if (ProjectionConstants.SCORE.Equals(projection[x]))
                    {
                        eip[x] = hits.Score(index);
                    }
                    else if (ProjectionConstants.ID.Equals(projection[x]))
                    {
                        eip[x] = entityInfo.Id;
                    }
                    else if (ProjectionConstants.DOCUMENT.Equals(projection[x]))
                    {
                        eip[x] = doc;
                    }
                    else if (ProjectionConstants.DOCUMENT_ID.Equals(projection[x]))
                    {
                        eip[x] = hits.Id(index);
                    }
                    else if (ProjectionConstants.BOOST.Equals(projection[x]))
                    {
                        eip[x] = doc.GetBoost();
                    }
                    else if (ProjectionConstants.THIS.Equals(projection[x]))
                    {
                        //THIS could be projected more than once
                        //THIS loading delayed to the Loader phase
                        if (entityInfo.IndexesOfThis == null)
                        {
                            entityInfo.IndexesOfThis = new List<int>(1);
                        }
                        entityInfo.IndexesOfThis.Add(x);
                    }
                }
            }
            return entityInfo;
        }
    }
}