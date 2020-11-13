using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;

namespace NHibernate.Search.Util
{
    /// <summary>
    /// 
    /// </summary>
    public class ScopedAnalyzer : Analyzer
    {
        private readonly IDictionary<string, Analyzer> scopedAnalyzers = new Dictionary<string, Analyzer>();
        private Analyzer globalAnalyzer;

        /// <summary>
        /// 
        /// </summary>
        public Analyzer GlobalAnalyzer
        {
            set { globalAnalyzer = value; }
        }

        public void AddScopedAnalyzer(string scope, Analyzer analyzer)
        {
            scopedAnalyzers.Add(scope, analyzer);
        }


        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            throw new NotImplementedException();
        }

        public override int GetPositionIncrementGap(string fieldName)
        {
            return GetAnalyzer(fieldName).GetPositionIncrementGap(fieldName);
        }

        private Analyzer GetAnalyzer(String fieldName)
        {
            Analyzer analyzer = null;
            if (scopedAnalyzers.ContainsKey(fieldName))
                analyzer = scopedAnalyzers[fieldName];

            // NB Think we need the re-test here as although the field might be mapped, it might not define an analyzer
            if (analyzer == null)
                analyzer = globalAnalyzer;

            return analyzer;
        }
    }
}