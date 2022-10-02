using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;

namespace NHibernate.Search.Tests.Analyzer
{
    public abstract class AbstractTestAnalyzer : Lucene.Net.Analysis.Analyzer
    {
        protected abstract string[] Tokens { get; }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new InternalTokenStream(Tokens);
        }

        #region Nested type: InternalTokenStream

        private class InternalTokenStream : TokenStream
        {
            private readonly string[] tokens;
            private int position;
            private readonly ITermAttribute termAttribute;

            public InternalTokenStream(string[] tokens)
            {
                this.tokens = tokens;
                termAttribute = AddAttribute<ITermAttribute>();
            }

            public override bool IncrementToken()
            {
                ClearAttributes();
                if (position < tokens.Length)
                {
                    termAttribute.SetTermBuffer(tokens[position++]);
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
            }
        }

        #endregion
    }
}