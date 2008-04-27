using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Search.Engine;
using NHibernate.Search.Event;

namespace NHibernate.Search.Util {
    internal static class ContextHelper {
        public static SearchFactory GetSearchFactory(ISession session) {
            return GetSearchFactoryBySFI((ISessionImplementor) session);
        }

        public static SearchFactory GetSearchFactoryBySFI(ISessionImplementor session) {
            IPostInsertEventListener[] listeners = session.Factory.EventListeners.PostInsertEventListeners;
            FullTextIndexEventListener listener = null;
            //FIXME this sucks since we mandante the event listener use
            foreach (IPostInsertEventListener candidate in listeners) {
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