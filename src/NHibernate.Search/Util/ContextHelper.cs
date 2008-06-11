using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Search.Event;
using NHibernate.Search.Impl;

namespace NHibernate.Search.Util
{
    internal static class ContextHelper
    {
        public static SearchFactoryImpl GetSearchFactory(ISession session)
        {
            return GetSearchFactoryBySFI((ISessionImplementor) session);
        }

        public static SearchFactoryImpl GetSearchFactoryBySFI(ISessionImplementor session)
        {
            IPostInsertEventListener[] listeners = session.Listeners.PostInsertEventListeners;
            FullTextIndexEventListener listener = null;
            //FIXME this sucks since we mandante the event listener use
            foreach (IPostInsertEventListener candidate in listeners)
            {
                if (candidate is FullTextIndexEventListener)
                    listener = (FullTextIndexEventListener) candidate;
                break;
            }

            if (listener == null)
                throw new HibernateException(
                    "Hibernate Search Event listeners not configured, please check the reference documentation and the " +
                    "application's hibernate.cfg.xml");
            return listener.SearchFactory;
        }
    }
}