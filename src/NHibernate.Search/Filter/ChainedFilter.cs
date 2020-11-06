using System;
using Lucene.Net.Util;

namespace NHibernate.Search.Filter
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A filter that performs a Boolean AND on multiple filters.
    /// </summary>
    public class ChainedFilter : Filter
    {
        private readonly List<Filter> chainedFilters = new List<Filter>();

        public void AddFilter(Filter filter)
        {
            chainedFilters.Add(filter);
        }

        private HashSet<int> DocIdSetToHashSet(DocIdSet docs)
        {
            var result = new HashSet<int>();
            var iterator = docs.GetIterator();

            int docId;
            while ((docId = iterator.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                result.Add(docId);

            return result;
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            if (chainedFilters.Count == 0)
            {
                throw new AssertionFailure("ChainedFilter has no filters to chain for");
            }

            // Create HashSet of first filter's contents
            HashSet<int> result = DocIdSetToHashSet(chainedFilters[0].GetDocIdSet(context, acceptDocs));

            // For each remaining filter, fill another HashSet and intersect it with the first.
            for (int i = 1; i < chainedFilters.Count; i++)
            {
                var nextSet = DocIdSetToHashSet(chainedFilters[i].GetDocIdSet(context, acceptDocs));
                result.IntersectWith(nextSet);
            }

            DocIdSet resultDocIds = new EnumerableBasedDocIdSet(result);
            return resultDocIds;
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("ChainedFilter [");
            foreach (Filter filter in chainedFilters)
            {
                sb.AppendLine().Append(filter);
            }

            return sb.Append("\r\n]").ToString();
        }
    }

    public class EnumerableBasedDocIdSet : DocIdSet
    {
        private readonly IEnumerable<int> _items;

        public EnumerableBasedDocIdSet(IEnumerable<int> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            _items = items;
        }

        /// <summary>
        /// Provides a <see cref="T:Lucene.Net.Search.DocIdSetIterator"/> to access the set.
        ///             This implementation can return <c>null</c> or
        ///             <c>EMPTY_DOCIDSET.Iterator()</c> if there
        ///             are no docs that match. 
        /// </summary>
        public override DocIdSetIterator GetIterator()
        {
            return new EnumerableBasedDocIdSetIterator(_items);
        }
    }

    public class EnumerableBasedDocIdSetIterator : DocIdSetIterator
    {
        private readonly IEnumerable<int> items;
        private IEnumerator<int> iterator;
        private int currentDocId = -1;

        public EnumerableBasedDocIdSetIterator(IEnumerable<int> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            this.items = items;
            iterator = items.GetEnumerator();
        }

        public override int Advance(int target)
        {
            if (target < currentDocId)
            {
                throw new ArgumentOutOfRangeException("target", target,
                    "Iterator state past target: " + currentDocId);
            }

            // Relies on NO_MORE_DOCS being a big number
            while (target > currentDocId)
            {
                currentDocId = iterator.MoveNext() ? iterator.Current : NO_MORE_DOCS;
            }

            return currentDocId;
        }

        public override int DocID
        {
            get
            {
                if (currentDocId == NO_MORE_DOCS || currentDocId == -1)
                {
                    return NO_MORE_DOCS;
                }

                return iterator.Current;
            }
        }

        public override long GetCost()
        {
            throw new NotImplementedException();
        }

        public override int NextDoc()
        {
            if (currentDocId == NO_MORE_DOCS)
            {
                return NO_MORE_DOCS;
            }

            return Advance(currentDocId + 1);
        }
    }
}