namespace NHibernate.Search {
	public interface IFullTextQuery :IQuery {
		int ResultSize { get; }
	}
}