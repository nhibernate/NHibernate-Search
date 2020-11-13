using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Analyzer
{
    public class Test3Analyzer : AbstractTestAnalyzer
    {
        private string[] tokens = { "music", "elephant", "energy" };

        protected override string[] Tokens
        {
            get { return tokens; }
        }

        /// <inheritdoc />
        public Test3Analyzer(LuceneVersion version) : base(version)
        {
        }
    }
}
