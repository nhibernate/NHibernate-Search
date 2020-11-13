using System;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Filter
{
    public class ExcludeAllFilter : Lucene.Net.Search.Filter
    {
        private static bool done = false;

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            throw new NotImplementedException();
            //if (done)
            //{
            //    throw new NotSupportedException("Called twice");
            //}

            //BitArray bitArray = new BitArray(reader.MaxDoc());
            //done = true;

            //return bitArray;
        }
    }
}
