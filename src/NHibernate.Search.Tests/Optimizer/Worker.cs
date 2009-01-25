using NHibernate.Search.Attributes;

namespace NHibernate.Search.Tests.Optimizer
{
    [Indexed]
    public class Worker
    {
        [DocumentId]
        private int id;

        [Field(Index.Tokenized)]
        private string name;

        [Field(Index.UnTokenized)]
        private int workhours;

        public Worker() { }

        public Worker(string name, int workhours)
        {
            this.name = name;
            this.workhours = workhours;
        }

        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual int Workhours
        {
            get { return workhours; }
            set { workhours = value; }
        }
    }
}