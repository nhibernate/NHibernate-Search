using System;
using System.Collections.Generic;
using System.Text;

namespace NHibernate.Search.Tests.Analyzer
{
    public class Test2Analyzer : AbstractTestAnalyzer
    {
        private string[] tokens = { "sound", "cat", "speed" };

        protected override string[] Tokens
        {
            get { return tokens; }
        }
    }
}
