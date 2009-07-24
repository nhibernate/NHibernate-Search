namespace NHibernate.Search.Filter
{
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
            {
                throw new AssertionFailure("ChainedFilter has no filters to chain for");
            }

            // We need to copy the first BitArray because BitArray is assigned to by And
            Filter filter = chainedFilters[0];
            BitArray result = (BitArray)filter.Bits(reader).Clone();
            int size = result.Count;
            for (int index = 1; index < chainedFilters.Count; index++ )
            {
                BitArray b2 = chainedFilters[index].Bits(reader);
                int s2 = b2.Count;
                if (s2 != size)
                {
                    // Align the lengths, any extra elements are set to false, ok as as we are Anding
                    b2.Length = size;
                }

                // Stared at this for hours - C# compiler doesn't warn when you discard a function result!
                result = result.And(b2);
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