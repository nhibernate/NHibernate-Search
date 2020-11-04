using System;
using NHibernate.Search.Attributes;
using Index = NHibernate.Search.Attributes.Index;

namespace NHibernate.Search.Tests.Filter
{
    [Indexed]
    [FullTextFilterDef("bestDriver", typeof(BestDriversFilter))] // Actual Filter implementation
    [FullTextFilterDef("security", typeof(SecurityFilterFactory))] // Filter factory with parameters
    [FullTextFilterDef("cachetest", typeof(ExcludeAllFilterFactory), Cache=true)] // Filter factory with parameters
    public class Driver 
    {
        [DocumentId]
        private int id;
        [Field(Index = Index.Tokenized)]
        private string name;
        [Field(Index = Index.UnTokenized)]
        private string teacher;
        [Field(Index = Index.UnTokenized)]
        private int score;
        [Field(Index = Index.UnTokenized)]
        [DateBridge(Resolution.Year)]
        private DateTime delivery;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Teacher
        {
            get { return teacher; }
            set { teacher = value; }
        }
        
        public int Score
        {
            get { return score; }
            set { score = value; }
        }

        public DateTime Delivery
        {
            get { return delivery; }
            set { delivery = value; }
        }

        public override int GetHashCode()
        {
            int result = id;
            result = 31*result + (name != null ? name.GetHashCode() : 0);
            result = 31*result + (teacher != null ? teacher.GetHashCode() : 0);
            result = 31*result + score;
            result = 31*result + (delivery != DateTime.MinValue ? delivery.GetHashCode() : 0);

            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Driver other = obj as Driver;
            if (other == null)
                return false;

            if (id != other.id) return false;
            if (score != other.score) return false;
            if (delivery != other.delivery) return false;

            return Equals(name, other.name) && Equals(teacher, other.teacher);
        }
    }

    public class TruckDriver : Driver
    {
        [Field(Index = Index.UnTokenized)]
        private string truckClass;

        public string TruckClass
        {
            get { return truckClass; }
            set { truckClass = value; }
        }
    }
}