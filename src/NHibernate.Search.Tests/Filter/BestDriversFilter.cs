using System.Collections;
using Lucene.Net.Index;

namespace NHibernate.Search.Tests.Filter
{
    public class BestDriversFilter : Lucene.Net.Search.Filter
    {
        public override BitArray Bits(IndexReader reader)
        {
            BitArray bitArray = new BitArray(reader.MaxDoc());
            TermDocs termDocs = reader.TermDocs(new Term("score", "5"));
            while (termDocs.Next())
            {
                bitArray.Set(termDocs.Doc(), true);
            }

            return bitArray;
        }
    }
}
