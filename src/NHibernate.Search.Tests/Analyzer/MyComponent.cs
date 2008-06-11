using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Analyzer
{
    public class MyComponent
    {
        [Field(Index.Tokenized)]
        [Analyzer(typeof(Test4Analyzer))]
        private string componentProperty;

        public string ComponentProperty
        {
            get { return componentProperty; }
            set { componentProperty = value; }
        }
    }
}