namespace NHibernate.Search.Backend
{
    public class PurgeAllLuceneWork : LuceneWork
    {
        public PurgeAllLuceneWork(System.Type clazz)
            : base(null, null, clazz, null)
        {
        }
    }
}