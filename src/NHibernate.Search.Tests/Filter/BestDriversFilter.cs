using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Filter
{
    public class BestDriversFilter : Lucene.Net.Search.Filter
    {
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            throw new NotImplementedException();
            //BitArray bitArray = new BitArray(reader.MaxDoc());
            //TermDocs termDocs = reader.TermDocs(new Term("score", "5"));
            //while (termDocs.Next())
            //{
            //    bitArray.Set(termDocs.Doc(), true);
            //}

            //return bitArray;
        }
    }
}
