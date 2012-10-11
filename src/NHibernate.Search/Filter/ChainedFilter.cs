using System;

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

        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            if (chainedFilters.Count == 0)
            {
                throw new AssertionFailure("ChainedFilter has no filters to chain for");
            }

            // We need to copy the first BitArray because BitArray is assigned to by And
            HashSet<int> result = null;

            foreach (var filter in chainedFilters)
            {
                DocIdSet b2 = filter.GetDocIdSet(reader);
                int docId;
                DocIdSetIterator iterator = b2.Iterator();

                if (result == null)
                {
                    result = new HashSet<int>();

                    while ((docId = iterator.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        result.Add(docId);
                    }
                }
                else
                {
                    while ((docId = iterator.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (!result.Contains(docId))
                        {
                            result.Remove(docId);
                        }
                    }
                }
            }

            DocIdSet filteredCombinedDocIdSet = new EnumerableBasedDocIdSet(result);
            return filteredCombinedDocIdSet;
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
        public override DocIdSetIterator Iterator()
        {
            return new EnumerableBasedDocIdSetIterator(_items);
        }
    }

    public class EnumerableBasedDocIdSetIterator : DocIdSetIterator
    {
        private readonly IEnumerable<int> items;
        private IEnumerator<int> iterator;
        private int? currentIndex;

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
            if (currentIndex == null)
            {
                currentIndex = 0;
            }

            if (target < currentIndex)
            {
                throw new ArgumentOutOfRangeException("target", target, "Iterator state past target: " + currentIndex);
            }

            int remaining = target - currentIndex.Value;
            bool hasMore;

            while ((hasMore = iterator.MoveNext()) && remaining > 0)
            {
                currentIndex++;
            }

            if (!hasMore)
            {
                currentIndex = NO_MORE_DOCS;
            }

            return currentIndex == NO_MORE_DOCS ? NO_MORE_DOCS : iterator.Current;
        }

        public override int DocID()
        {
            if (currentIndex == NO_MORE_DOCS || currentIndex == null)
            {
                return NO_MORE_DOCS;
            }

            return iterator.Current;
        }

        public override int NextDoc()
        {
            if (currentIndex == NO_MORE_DOCS)
            {
                return NO_MORE_DOCS;
            }

            return Advance(currentIndex.Value + 1);
        }
    }
}