using System;
using System.Collections;
using Lucene.Net.Index;

namespace NHibernate.Search.Tests.Filter
{
    public class ExcludeAllFilter : Lucene.Net.Search.Filter
    {
        private static bool done = false;

        public override BitArray Bits(IndexReader reader)
        {
            if (done)
            {
                throw new NotSupportedException("Called twice");
            }

            BitArray bitArray = new BitArray(reader.MaxDoc());
            done = true;

            return bitArray;
        }
    }
}
