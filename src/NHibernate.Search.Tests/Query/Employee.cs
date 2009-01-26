namespace NHibernate.Search.Tests.Query
{
    using Attributes;

    [Indexed]
    public class Employee
    {
        private int id;
        private string lastname;
        private string dept;

        public Employee()
        {            
        }

        public Employee(int id, string lastname, string dept)
        {
            this.id = id;
            this.lastname = lastname;
            this.dept = dept;
        }

        [DocumentId]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        [Field(Index = Index.No, Store = Store.Yes)]
        public string Lastname
        {
            get { return lastname; }
            set { lastname = value; }
        }

        [Field(Index = Index.Tokenized, Store = Store.Yes)]
        public string Dept
        {
            get { return dept; }
            set { dept = value; }
        }
    }
}
