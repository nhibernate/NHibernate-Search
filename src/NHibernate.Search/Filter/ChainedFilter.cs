namespace NHibernate.Search.Filter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    using Lucene.Net.Index;
    using Lucene.Net.Search;

    public class ChainedFilter : Filter
    {
        private readonly List<Filter> chainedFilters = new List<Filter>();

        public void AddFilter(Filter filter)
        {
            chainedFilters.Add(filter);
        }

        public override BitArray Bits(IndexReader reader)
        {
            if (chainedFilters.Count == 0)
                throw new AssertionFailure("ChainedFilter has no filters to chain for");

            // We need to copy the first BitArray because BitArray is modified by .logicalOp
            Filter filter = chainedFilters[0];
            BitArray result = (BitArray)filter.Bits(reader).Clone();
            for (int index = 1; index < chainedFilters.Count - 1; index++ )
            {
                result.And(chainedFilters[index].Bits(reader));
            }

            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("ChainedFilter [");
            foreach (Filter filter in chainedFilters)
            {
                sb.AppendLine().Append(filter.ToString());
            }

            return sb.Append("\r\n]").ToString();
        }
    }
}