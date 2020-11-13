using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Analyzer
{
    public abstract class AbstractTestAnalyzer : Lucene.Net.Analysis.Analyzer
    {
        private readonly LuceneVersion _version;

        protected AbstractTestAnalyzer(LuceneVersion version)
        {
            _version = version;
        }

        protected abstract string[] Tokens { get; }

        /// <inheritdoc />
        protected override TokenStreamComponents CreateComponents(String fieldName, TextReader reader) =>
            new TokenStreamComponents(
                new StandardTokenizer(_version, reader), new InternalTokenStream(Tokens));

        #region Nested type: InternalTokenStream

        private sealed class InternalTokenStream : TokenStream
        {
            private readonly string[] tokens;
            private int position;

            public InternalTokenStream(string[] tokens)
            {
                this.tokens = tokens;
            }

            /// <inheritdoc />
            public override Boolean IncrementToken()
            {
                if (position < tokens.Length)
                {
                    position++;
                    return true;
                }

                return false;
            }
        }

        #endregion
    }
}