using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using NHibernate.Proxy;
using NHibernate.Search.Backend;
using NHibernate.Search.Bridge;
using NHibernate.Search.Impl;
using NHibernate.Search.Mapping;
using NHibernate.Search.Store;
using NHibernate.Search.Util;
using NHibernate.Util;
using Index = NHibernate.Search.Attributes.Index;

namespace NHibernate.Search.Engine
{
    /// <summary>
    /// Set up and provide a manager for indexes classes
    /// </summary>
    public class DocumentBuilder
    {
        public const string CLASS_FIELDNAME = "_hibernate_class";
        private static readonly IInternalLogger logger = LoggerProvider.LoggerFor(typeof(DocumentBuilder));

        private readonly IDirectoryProvider[] directoryProviders;
        private readonly IIndexShardingStrategy shardingStrategy;
        private readonly ScopedAnalyzer analyzer;
        private DocumentIdMapping idMapping;
        private ISet<System.Type> mappedSubclasses = new HashSet<System.Type>();

        private readonly DocumentMapping rootClassMapping;

        public DocumentBuilder(DocumentMapping classMapping, Analyzer defaultAnalyzer, IDirectoryProvider[] directoryProviders,
                               IIndexShardingStrategy shardingStrategy)
        {
            analyzer = new ScopedAnalyzer();
            this.directoryProviders = directoryProviders;
            this.shardingStrategy = shardingStrategy;

            if (classMapping == null) throw new AssertionFailure("Unable to build a DocumemntBuilder with a null class");

            rootClassMapping = classMapping;

            ISet<System.Type> processedClasses = new HashSet<System.Type>();
            processedClasses.Add(classMapping.MappedClass);
            CollectAnalyzers(rootClassMapping, defaultAnalyzer, true, string.Empty, processedClasses);
            //processedClasses.remove( clazz ); for the sake of completness
            analyzer.GlobalAnalyzer = defaultAnalyzer;
            if (idMapping == null)
                throw new SearchException("No document id for: " + classMapping.MappedClass.Name);
        }

        public Analyzer Analyzer => analyzer;

        public IDirectoryProvider[] DirectoryProviders => directoryProviders;

        public IIndexShardingStrategy DirectoryProvidersSelectionStrategy => shardingStrategy;

        public ITwoWayFieldBridge IdBridge => idMapping.Bridge;

        public ISet<System.Type> MappedSubclasses => mappedSubclasses;

        public string IdentifierName => idMapping.PropertyName;


        /// <summary>
        /// This add the new work to the queue, so it can be processed in a batch fashion later
        /// </summary>
        public void AddToWorkQueue(System.Type entityClass, object entity, object id, WorkType workType,
                                   List<LuceneWork> queue,
                                   ISearchFactoryImplementor searchFactoryImplementor)
        {
            // TODO with the caller loop we are in a n^2: optimize it using a HashMap for work recognition
            foreach (LuceneWork luceneWork in queue)
            {
                if (luceneWork.EntityClass == entityClass && luceneWork.Id.Equals(id))
                {
                    return;
                }
            }

            bool searchForContainers = false;
            string idString = idMapping.Bridge.ObjectToString(id);

            switch (workType)
            {
                case WorkType.Add:
                    queue.Add(new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id, entityClass)));
                    searchForContainers = true;
                    break;

                case WorkType.Delete:
                case WorkType.Purge:
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    break;

                case WorkType.PurgeAll:
                    queue.Add(new PurgeAllLuceneWork((System.Type)entity));
                    break;

                case WorkType.Update:
                case WorkType.Collection:
                    /**
                     * even with Lucene 2.1, use of indexWriter to update is not an option
                     * We can only delete by term, and the index doesn't have a term that
                     * uniquely identify the entry.
                     * But essentially the optimization we are doing is the same Lucene is doing, the only extra cost is the
                     * double file opening.
                    */
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    queue.Add(new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id, entityClass)));
                    searchForContainers = true;
                    break;

                case WorkType.Index:
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    LuceneWork work = new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id, entityClass));
                    work.IsBatch = true;
                    queue.Add(work);
                    searchForContainers = true;
                    break;

                default:
                    throw new AssertionFailure("Unknown WorkType: " + workType);
            }

            /**
		     * When references are changed, either null or another one, we expect dirty checking to be triggered (both sides
		     * have to be updated)
		     * When the internal object is changed, we apply the {Add|Update}Work on containedIns
		    */
            if (searchForContainers)
            {
                ProcessContainedIn(entity, queue, rootClassMapping, searchFactoryImplementor);
            }
        }

        public Document GetDocument(object instance, object id, System.Type entityType)
        {
            var doc = new Document();

            // TODO: Check if that should be an else?
            idMapping.Bridge.Set(idMapping.Name, id, doc, Field.Store.YES);

            BuildDocumentFields(instance, doc, rootClassMapping, string.Empty);
            return doc;
        }

        public Term GetTerm(object id)
        {
            return new Term(GetIdKeywordName(), IdBridge.ObjectToString(id));
        }

        public String GetIdKeywordName()
        {
            return idMapping.Name;
        }

        public static System.Type GetDocumentClass(Document document)
        {
            string className = document.Get(CLASS_FIELDNAME);
            try
            {
                return ReflectHelper.ClassForName(className);
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to load indexed class: " + className, e);
            }
        }

        public static object GetDocumentId(ISearchFactoryImplementor searchFactory, System.Type clazz, Document document)
        {
            DocumentBuilder builder = searchFactory.DocumentBuilders[clazz];
            if (builder == null)
            {
                throw new SearchException("No Lucene configuration set up for: " + clazz.Name);
            }

            return builder.IdBridge.Get(builder.GetIdKeywordName(), document);
        }

        public static object[] GetDocumentFields(ISearchFactoryImplementor searchFactoryImplementor, System.Type clazz,
                                                 Document document, string[] fields)
        {
            DocumentBuilder builder;
            if (!searchFactoryImplementor.DocumentBuilders.TryGetValue(clazz, out builder))
            {
                throw new SearchException("No Lucene configuration set up for: " + clazz.Name);
            }

            object[] result = new object[fields.Length];
            if (builder.idMapping != null)
            {
                PopulateResult(builder.IdentifierName, builder.IdBridge, Attributes.Store.Yes, fields, result, document);
            }

            ProcessFieldsForProjection(builder.rootClassMapping, fields, result, document);

            return result;
        }

        public void PostInitialize(ISet<System.Type> indexedClasses)
        {
            // this method does not requires synchronization
            System.Type plainClass = rootClassMapping.MappedClass;
            ISet<System.Type> tempMappedSubclasses = new HashSet<System.Type>();

            // together with the caller this creates a o(2), but I think it's still faster than create the up hierarchy for each class
            foreach (System.Type currentClass in indexedClasses)
            {
                if (plainClass.IsAssignableFrom(currentClass))
                {
                    tempMappedSubclasses.Add(currentClass);
                }
            }

            mappedSubclasses = tempMappedSubclasses;
        }

        private void BuildDocumentFields(Object instance, Document doc, DocumentMapping classMapping, string prefix)
        {
            if (instance == null)
            {
                return;
            }

            var unproxiedInstance = Unproxy(instance);
            foreach (var bridge in classMapping.ClassBridges)
            {
                var bridgeName = prefix + bridge.Name;
                try
                {
                    bridge.Bridge.Set(
                        bridgeName,
                        unproxiedInstance,
                        doc,
                        GetStore(bridge.Store)
                    );
                }
                catch (Exception e)
                {
                    logger.Error(
                        string.Format(CultureInfo.InvariantCulture, "Error processing class bridge for {0}",
                                      bridgeName), e);
                }
            }

            foreach (var field in classMapping.Fields)
            {
                BuildDocumentField(field, unproxiedInstance, doc, prefix);
            }

            foreach (var embedded in classMapping.Embedded)
            {
                BuildDocumentFieldsForEmbedded(embedded, unproxiedInstance, doc, prefix);
            }
        }

        private void BuildDocumentField(FieldMapping fieldMapping, object unproxiedInstance, Document doc, string prefix)
        {
            var value = fieldMapping.Getter.Get(unproxiedInstance);
            var fieldName = prefix + fieldMapping.Name;
            try
            {
                fieldMapping.Bridge.Set(
                    fieldName,
                    value,
                    doc,
                    GetStore(fieldMapping.Store)
                );
            }
            catch (Exception e)
            {
                logger.Error(
                    string.Format(CultureInfo.InvariantCulture, "Error processing field bridge for {0}.{1}",
                                  unproxiedInstance.GetType().FullName, fieldName), e);
            }
        }

        private void BuildDocumentFieldsForEmbedded(EmbeddedMapping embeddedMapping, object unproxiedInstance, Document doc, string prefix)
        {
            var value = embeddedMapping.Getter.Get(unproxiedInstance);
            if (value == null)
                return;

            try
            {
                prefix += embeddedMapping.Prefix;
                if (value is IDictionary)
                {
                    foreach (object collectionValue in (value as IDictionary).Values)
                    {
                        BuildDocumentFields(collectionValue, doc, embeddedMapping.Class, prefix);
                    }
                }
                else if (value is IEnumerable)
                {
                    foreach (object collectionValue in value as IEnumerable)
                    {
                        BuildDocumentFields(collectionValue, doc, embeddedMapping.Class, prefix);
                    }
                }
                else
                {
                    BuildDocumentFields(value, doc, embeddedMapping.Class, prefix);
                }
            }
            catch (NullReferenceException)
            {
                logger.Error(string.Format("Null reference while processing {0}.{1}, property type {2}",
                                           unproxiedInstance.GetType().FullName,
                                           prefix, value.GetType()));
            }
        }

        private static int GetFieldPosition(string[] fields, string fieldName)
        {
            int fieldNbr = fields.GetUpperBound(0);
            for (int index = 0; index < fieldNbr; index++)
            {
                if (fieldName.Equals(fields[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private static Field.Store GetStore(Attributes.Store store)
        {
            switch (store)
            {
                case Attributes.Store.No:
                    return Field.Store.NO;
                case Attributes.Store.Yes:
                    return Field.Store.YES;
                default:
                    throw new AssertionFailure("Unexpected Store: " + store);
            }
        }

        private void CollectAnalyzers(
            DocumentMapping @class, Analyzer parentAnalyzer, bool isRoot, string prefix, ISet<System.Type> processedClasses
        )
        {
            foreach (var bridge in @class.ClassBridges)
            {
                var bridgeAnalyzer = bridge.Analyzer ?? parentAnalyzer;
                if (bridgeAnalyzer == null)
                {
                    throw new NotSupportedException("Analyzer should not be undefined");
                }

                analyzer.AddScopedAnalyzer(prefix + bridge.Name, bridgeAnalyzer);
            }

            if (isRoot && @class.DocumentId != null)
            {
                idMapping = @class.DocumentId;
            }

            foreach (var field in @class.Fields)
            {
                CollectAnalyzer(field, parentAnalyzer, prefix);
            }

            foreach (var embedded in @class.Embedded)
            {
                CollectAnalyzers(
                    embedded.Class, parentAnalyzer, false, prefix + embedded.Prefix, processedClasses
                );
            }
        }

        private void CollectAnalyzer(FieldMapping field, Analyzer parentAnalyzer, string prefix)
        {
            // Field > property > entity analyzer
            var localAnalyzer = field.Analyzer ?? parentAnalyzer;
            if (localAnalyzer == null)
                throw new NotSupportedException("Analyzer should not be undefined");

            analyzer.AddScopedAnalyzer(prefix + field.Name, localAnalyzer);
        }

        private static void ProcessFieldsForProjection(
            DocumentMapping mapping, String[] fields, Object[] result, Document document
        )
        {
            foreach (var field in mapping.Fields)
            {
                PopulateResult(
                    field,
                    fields,
                    result,
                    document
               );
            }

            foreach (var embedded in mapping.Embedded)
            {
                if (!embedded.IsCollection)
                    ProcessFieldsForProjection(embedded.Class, fields, result, document);
            }
        }

        private static void PopulateResult(FieldMapping field, string[] fields, object[] result, Document document)
        {
            PopulateResult(field.Name, field.Bridge, field.Store, fields, result, document);
        }

        private static void PopulateResult(
            string fieldName, IFieldBridge fieldBridge, Attributes.Store fieldStore,
            string[] fields, object[] result, Document document
        )
        {
            int matchingPosition = GetFieldPosition(fields, fieldName);
            if (matchingPosition != -1)
            {
                //TODO make use of an isTwoWay() method
                if (fieldStore != Attributes.Store.No && fieldBridge is ITwoWayFieldBridge)
                {
                    result[matchingPosition] = ((ITwoWayFieldBridge)fieldBridge).Get(fieldName, document);
                    if (logger.IsInfoEnabled)
                    {
                        logger.Info("Field " + fieldName + " projected as " + result[matchingPosition]);
                    }
                }
                else
                {
                    if (fieldStore == Attributes.Store.No)
                    {
                        throw new SearchException("Projecting an unstored field: " + fieldName);
                    }

                    throw new SearchException("IFieldBridge is not a ITwoWayFieldBridge: " + fieldBridge.GetType());
                }
            }
        }

        private static void ProcessContainedIn(Object instance, List<LuceneWork> queue, DocumentMapping documentMapping,
                                               ISearchFactoryImplementor searchFactoryImplementor)
        {
            foreach (var containedIn in documentMapping.ContainedIn)
            {
                object value = containedIn.Getter.Get(instance);

                if (value == null) continue;

                Array array = value as Array;
                if (array != null)
                {
                    foreach (object arrayValue in array)
                    {
                        // Highly inneficient but safe wrt the actual targeted class, e.g. polymorphic items in the array
                        System.Type valueType = NHibernateUtil.GetClass(arrayValue);
                        if (valueType == null || !searchFactoryImplementor.DocumentBuilders.ContainsKey(valueType))
                        {
                            continue;
                        }

                        ProcessContainedInValue(arrayValue, queue, valueType, searchFactoryImplementor.DocumentBuilders[valueType],
                                                searchFactoryImplementor);
                    }
                }
                else if (typeof(IEnumerable).IsAssignableFrom(value.GetType()))
                {
                    // NB We only see ISet and IDictionary`2 as IEnumerable
                    IEnumerable collection = value as IEnumerable;
                    if (typeof(IDictionary).IsAssignableFrom(value.GetType()))
                    {
                        collection = ((IDictionary)value).Values;
                    }

                    if (collection == null)
                    {
                        continue;
                    }

                    foreach (object collectionValue in collection)
                    {
                        // Highly inneficient but safe wrt the actual targeted class, e.g. polymorphic items in the array
                        System.Type valueType = NHibernateUtil.GetClass(collectionValue);
                        if (valueType == null || !searchFactoryImplementor.DocumentBuilders.ContainsKey(valueType))
                        {
                            continue;
                        }

                        ProcessContainedInValue(collectionValue, queue, valueType,
                                                searchFactoryImplementor.DocumentBuilders[valueType], searchFactoryImplementor);
                    }
                }
                else
                {
                    System.Type valueType = NHibernateUtil.GetClass(value);
                    if (valueType == null || !searchFactoryImplementor.DocumentBuilders.ContainsKey(valueType))
                    {
                        continue;
                    }

                    ProcessContainedInValue(value, queue, valueType, searchFactoryImplementor.DocumentBuilders[valueType],
                                            searchFactoryImplementor);
                }
            }

            //an embedded cannot have a useful @ContainedIn (no shared reference)
            //do not walk through them
        }

        private static void ProcessContainedInValue(object value, List<LuceneWork> queue, System.Type valueClass,
                                                    DocumentBuilder builder, ISearchFactoryImplementor searchFactory)
        {
            object id = builder.idMapping.Getter.Get(value);
            builder.AddToWorkQueue(valueClass, value, id, WorkType.Update, queue, searchFactory);
        }

        private static object Unproxy(object value)
        {
            var proxy = value as INHibernateProxy;
            return proxy == null ? value : proxy.HibernateLazyInitializer.GetImplementation();
        }
    }
}