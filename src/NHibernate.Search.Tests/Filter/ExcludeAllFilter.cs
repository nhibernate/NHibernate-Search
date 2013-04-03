using System;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace NHibernate.Search.Tests.Filter
{
    public class ExcludeAllFilter : Lucene.Net.Search.Filter
    {
        private static bool done = false;

        public override DocIdSet GetDocIdSet(IndexReader reader)
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
