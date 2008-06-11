using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Analyzer
{
    [Indexed]
    [Analyzer(typeof(Test1Analyzer))]
    public class MyEntity
    {
        [DocumentId]
        private int id;

        [Field(Index.Tokenized)]
        private string entity;

        [Field(Index.Tokenized)]
        [Analyzer(typeof(Test2Analyzer))]
        private string property;

        [Field(Index.Tokenized, Analyzer=typeof(Test3Analyzer))]
        [Analyzer(typeof(Test2Analyzer))]
        private string field;

        [IndexedEmbedded]
        private MyComponent component;

        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string Entity
        {
            get { return entity; }
            set { entity = value; }
        }

        public virtual string Property
        {
            get { return property; }
            set { property = value; }
        }

        public virtual string Field
        {
            get { return field; }
            set { field = value; }
        }

        public virtual MyComponent Component
        {
            get { return component; }
            set { component = value; }
        }
    }
}
