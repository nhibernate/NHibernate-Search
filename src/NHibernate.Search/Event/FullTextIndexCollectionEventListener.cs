namespace NHibernate.Search.Event
{
    using Backend;

    using NHibernate.Engine;
    using NHibernate.Event;

    /// <summary>
    /// Support collection event listening
    /// </summary>
    /// HACK: Deprecate as soon as we target Core 3.3 and merge back into the superclass
    public class FullTextIndexCollectionEventListener : FullTextIndexEventListener, 
                                    IPostCollectionRecreateEventListener, IPostCollectionRemoveEventListener, IPostCollectionUpdateEventListener
    {
		private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(FullTextIndexCollectionEventListener));

        #region Public methods

        public void OnPostRecreateCollection(PostCollectionRecreateEvent @event)
        {
            ProcessCollectionEvent(@event);
        }

        public void OnPostRemoveCollection(PostCollectionRemoveEvent @event)
        {
            ProcessCollectionEvent(@event);
        }

        public void OnPostUpdateCollection(PostCollectionUpdateEvent @event)
        {
            ProcessCollectionEvent(@event);
        }

        #endregion

        #region Private methods

        private object GetId(object entity, AbstractCollectionEvent @event)
        {
            object id = @event.AffectedOwnerIdOrNull;
            if (id == null)
            {
			    // most likely this recovery is unnecessary since Hibernate Core probably try that 
                EntityEntry entityEntry = (EntityEntry) @event.Session.PersistenceContext.EntityEntries[entity];
                id = entityEntry == null ? null : entityEntry.Id;
            }

            return id;
        }

        private void ProcessCollectionEvent(AbstractCollectionEvent @event)
        {
            object entity = @event.AffectedOwnerOrNull;
            if (entity == null)
            {
                // Hibernate cannot determine every single time the owner especially incase detached objects are involved
                // or property-ref is used
                // Should log really but we don't know if we're interested in this collection for indexing
                return;
            }

            if (used && EntityIsIndexed(entity))
            {
                object id = GetId(entity, @event);
                if (id == null)
                {
                    log.Warn("Unable to reindex entity on collection change, id cannot be extracted: " + @event.GetAffectedOwnerEntityName());
                    return;
                }

                ProcessWork(entity, id, WorkType.Collection, @event);
            }
        }

        #endregion
    }
}