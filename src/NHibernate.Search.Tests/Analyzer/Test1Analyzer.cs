using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Util;

namespace NHibernate.Search.Tests.Analyzer
{
    public class Test1Analyzer : AbstractTestAnalyzer
    {
        private string[] tokens = { "alarm", "dog", "performance" };

        protected override string[] Tokens
        {
            get { return tokens; }
        }

        /// <inheritdoc />
        public Test1Analyzer(LuceneVersion version) : base(version)
        {
        }
    }
}
