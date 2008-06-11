using System.IO;
using Lucene.Net.Analysis;

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

            public InternalTokenStream(string[] tokens)
            {
                this.tokens = tokens;
            }

            public override Token Next()
            {
                return position >= tokens.Length ? null : new Token(tokens[position++], 0, 0);
            }
        }

        #endregion
    }
}