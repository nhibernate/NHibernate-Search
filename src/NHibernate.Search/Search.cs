using NHibernate.Search.Impl;

namespace NHibernate.Search
{
    public static class Search
    {
        public static IFullTextSession CreateFullTextSession(ISession session)
        {
            return session as FullTextSessionImpl ??
                new FullTextSessionImpl(session);
        }
    }
}
