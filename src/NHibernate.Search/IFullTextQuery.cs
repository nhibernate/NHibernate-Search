using Lucene.Net.Search;

namespace NHibernate.Search {
	public interface IFullTextQuery :IQuery {
		int ResultSize { get; }

	    Sort Sort {
	        set;
	    }
	}
}