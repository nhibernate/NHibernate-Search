using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Analyzer
{
    public abstract class AbstractTestAnalyzer : Lucene.Net.Analysis.Analyzer
    {
        protected abstract string[] Tokens { get; }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return new TokenStreamComponents(
                new StandardTokenizer(LuceneVersion.LUCENE_48, reader),
                new InternalTokenStream(Tokens));
        }

        private sealed class InternalTokenStream : TokenStream
        {
            private readonly string[] tokens;
            private int position;
            private readonly IPayloadAttribute attribute;

            public InternalTokenStream(string[] tokens)
            {
                this.tokens = tokens;
                attribute = AddAttribute<IPayloadAttribute>();
            }

            public override bool IncrementToken()
            {
                ClearAttributes();
                if (position < tokens.Length)
                {
                    attribute.Payload = new BytesRef(tokens[position++]);
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
            }
        }
    }
}