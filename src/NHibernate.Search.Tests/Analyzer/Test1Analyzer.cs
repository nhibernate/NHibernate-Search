using System;
using System.Collections.Generic;
using System.Text;

namespace NHibernate.Search.Tests.Analyzer
{
    public class Test1Analyzer : AbstractTestAnalyzer
    {
        private string[] tokens = { "alarm", "dog", "performance" };

        protected override string[] Tokens
        {
            get { return tokens; }
        }
    }
}
