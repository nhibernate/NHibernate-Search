using System;
using System.Collections.Generic;
using System.Reflection;
using Iesi.Collections.Generic;
using log4net;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using NHibernate.Search.Attributes;
using NHibernate.Search.Backend;
using NHibernate.Search.Bridge;
using NHibernate.Search.Impl;
using NHibernate.Search.Storage;
using NHibernate.Search.Util;
using NHibernate.Util;
using FieldInfo=System.Reflection.FieldInfo;

namespace NHibernate.Search.Engine
{
    /// <summary>
    /// Set up and provide a manager for indexes classes
    /// </summary>
    public class DocumentBuilder
    {
        public const string CLASS_FIELDNAME = "_hibernate_class";
        private static readonly ILog log = LogManager.GetLogger(typeof(DocumentBuilder));

        private readonly PropertiesMetadata rootPropertiesMetadata;
        private readonly System.Type beanClass;
        private readonly IDirectoryProvider directoryProvider;
        private String idKeywordName;
        private MemberInfo idGetter;
        private float? idBoost;
        private ITwoWayFieldBridge idBridge;
        private ISet<System.Type> mappedSubclasses = new HashedSet<System.Type>();
        private int level;
        private int maxLevel = int.MaxValue;
        private ScopedAnalyzer analyzer;

        #region Nested type: PropertiesMetadata

        private class PropertiesMetadata
        {
            public readonly List<float> classBoosts = new List<float>();
            public readonly List<IFieldBridge> classBridges = new List<IFieldBridge>();
            public readonly List<Field.Index> classIndexes = new List<Field.Index>();
            public readonly List<string> classNames = new List<string>();
            public readonly List<Field.Store> classStores = new List<Field.Store>();
            public readonly List<MemberInfo> containedInGetters = new List<MemberInfo>();
            public readonly List<MemberInfo> embeddedContainers = new List<MemberInfo>();
            public readonly List<MemberInfo> embeddedGetters = new List<MemberInfo>();
            public readonly List<PropertiesMetadata> embeddedPropertiesMetadata = new List<PropertiesMetadata>();
            public readonly List<IFieldBridge> fieldBridges = new List<IFieldBridge>();
            public readonly List<MemberInfo> fieldGetters = new List<MemberInfo>();
            public readonly List<Field.Index> fieldIndex = new List<Field.Index>();
            public readonly List<String> fieldNames = new List<String>();
            public readonly List<Field.Store> fieldStore = new List<Field.Store>();
            public Analyzer analyzer;
            public float? boost;
        }

        #endregion

        #region Constructors

        public DocumentBuilder(System.Type clazz, Analyzer defaultAnalyzer, IDirectoryProvider directory)
        {
            analyzer = new ScopedAnalyzer();
            //analyzer = defaultAnalyzer;
            beanClass = clazz;
            directoryProvider = directory;

            if (clazz == null) throw new AssertionFailure("Unable to build a DocumemntBuilder with a null class");

            rootPropertiesMetadata = new PropertiesMetadata();
            rootPropertiesMetadata.boost = GetBoost(clazz);
            rootPropertiesMetadata.analyzer = defaultAnalyzer;

            Set<System.Type> processedClasses = new HashedSet<System.Type>();
            processedClasses.Add(clazz);
            InitializeMembers(clazz, rootPropertiesMetadata, true, "", processedClasses);
            //processedClasses.remove( clazz ); for the sake of completness

            analyzer.GlobalAnalyzer = rootPropertiesMetadata.analyzer;
            if (idKeywordName == null)
                throw new SearchException("No document id for: " + clazz.Name);
        }

        #endregion

        #region Property methods

        public Analyzer Analyzer
        {
            get { return analyzer; }
        }

        public IDirectoryProvider DirectoryProvider
        {
            get { return directoryProvider; }
        }

        public ITwoWayFieldBridge IdBridge
        {
            get { return idBridge; }
        }

        public ISet<System.Type> MappedSubclasses
        {
            get { return mappedSubclasses; }
        }

        #endregion

        #region Private methods

        private void BindClassAnnotation(string prefix, PropertiesMetadata propertiesMetadata, ClassBridgeAttribute ann)
        {
            // TODO: Name should be prefixed - NB is this still true?
            string fieldName = prefix + ann.Name;
            propertiesMetadata.classNames.Add(fieldName);
            propertiesMetadata.classStores.Add(GetStore(ann.Store));
            propertiesMetadata.classIndexes.Add(GetIndex(ann.Index));
            propertiesMetadata.classBridges.Add(BridgeFactory.ExtractType(ann));
            propertiesMetadata.classBoosts.Add(ann.Boost);

            Analyzer classAnalyzer = GetAnalyzer(ann.Analyzer) ?? propertiesMetadata.analyzer;
            if (classAnalyzer == null)
                throw new NotSupportedException("Analyzer should not be undefined");

            analyzer.AddScopedAnalyzer(fieldName, classAnalyzer);
        }

        private void BindFieldAnnotation(MemberInfo member, PropertiesMetadata propertiesMetadata, string prefix,
                                         FieldAttribute fieldAnn)
        {
            SetAccessible(member);
            propertiesMetadata.fieldGetters.Add(member);
            string fieldName = prefix + BinderHelper.GetAttributeName(member, fieldAnn.Name);
            propertiesMetadata.fieldNames.Add(prefix + fieldAnn.Name);
            propertiesMetadata.fieldStore.Add(GetStore(fieldAnn.Store));
            propertiesMetadata.fieldIndex.Add(GetIndex(fieldAnn.Index));
            propertiesMetadata.fieldBridges.Add(BridgeFactory.GuessType(member));

            Analyzer memberAnalyzer = GetAnalyzer(member) ?? propertiesMetadata.analyzer;
            if (memberAnalyzer == null)
                throw new NotSupportedException("Analyzer should not be undefined");

            analyzer.AddScopedAnalyzer(fieldName, memberAnalyzer);
        }

        private static Analyzer GetAnalyzer(MemberInfo member)
        {
            return null;
        }

        private static Analyzer GetAnalyzer(System.Type analyzerType)
        {
            if (analyzerType == null)
                return null;

            if (!typeof(Analyzer).IsAssignableFrom(analyzerType))
                throw new SearchException("Lucene analyzer not implemented by " + analyzerType.FullName);

            try
            {
                return (Analyzer) Activator.CreateInstance(analyzerType);
            }
            catch
            {
                // TODO: See if we can get a tigher exception trap here
                throw new SearchException("Failed to instantiate lucene analyzer with type  " + analyzerType.FullName);
            }
        }

        private static float? GetBoost(MemberInfo element)
        {
            if (element == null) return null;
            BoostAttribute boost = AttributeUtil.GetBoost(element);
            if (boost == null)
                return null;
            return boost.Value;
        }

        private static Field.Index GetIndex(Index index)
        {
            switch (index)
            {
                case Index.No:
                    return Field.Index.NO;
                case Index.NoNorms:
                    return Field.Index.NO_NORMS;
                case Index.Tokenized:
                    return Field.Index.TOKENIZED;
                case Index.UnTokenized:
                    return Field.Index.UN_TOKENIZED;
                default:
                    throw new AssertionFailure("Unexpected Index: " + index);
            }
        }

        private static object GetMemberValue(Object instnace, MemberInfo getter)
        {
            PropertyInfo info = getter as PropertyInfo;
            return info != null ? info.GetValue(instnace, null) : ((FieldInfo) getter).GetValue(instnace);
        }

        private static Field.Store GetStore(Attributes.Store store)
        {
            switch (store)
            {
                case Attributes.Store.No:
                    return Field.Store.NO;
                case Attributes.Store.Yes:
                    return Field.Store.YES;
                case Attributes.Store.Compress:
                    return Field.Store.COMPRESS;
                default:
                    throw new AssertionFailure("Unexpected Store: " + store);
            }
        }

        private void InitializeMember(
            MemberInfo member, PropertiesMetadata propertiesMetadata, bool isRoot,
            String prefix, ISet<System.Type> processedClasses)
        {
            DocumentIdAttribute documentIdAnn = AttributeUtil.GetDocumentId(member);
            if (documentIdAnn != null)
            {
                if (isRoot)
                {
                    if (idKeywordName != null)
                        if (documentIdAnn.Name != null)
                            throw new AssertionFailure("Two document id assigned: "
                                                       + idKeywordName + " and " + documentIdAnn.Name);
                    idKeywordName = prefix + documentIdAnn.Name;
                    IFieldBridge fieldBridge = BridgeFactory.GuessType(member);
                    if (fieldBridge is ITwoWayFieldBridge)
                        idBridge = (ITwoWayFieldBridge) fieldBridge;
                    else
                        throw new SearchException(
                            "Bridge for document id does not implement IdFieldBridge: " + member.Name);
                    idBoost = GetBoost(member);
                    idGetter = member;
                }
                else
                {
                    // Component should index their document id
                    SetAccessible(member);
                    propertiesMetadata.fieldGetters.Add(member);
                    string fieldName = prefix + BinderHelper.GetAttributeName(member, documentIdAnn.Name);
                    propertiesMetadata.fieldNames.Add(fieldName);
                    propertiesMetadata.fieldStore.Add(GetStore(Attributes.Store.Yes));
                    propertiesMetadata.fieldIndex.Add(GetIndex(Index.UnTokenized));
                    propertiesMetadata.fieldBridges.Add(BridgeFactory.GuessType(member));

                    // Field > property > entity analyzer
                    Analyzer memberAnalyzer = GetAnalyzer(member) ?? propertiesMetadata.analyzer;
                    if (memberAnalyzer == null)
                        throw new NotSupportedException("Analyzer should not be undefined");

                    analyzer.AddScopedAnalyzer(fieldName, memberAnalyzer);
                }
            }

            List<FieldAttribute> fieldAttributes = AttributeUtil.GetFields(member);
            if (fieldAttributes != null)
            {
                foreach (FieldAttribute fieldAnn in fieldAttributes)
                    BindFieldAnnotation(member, propertiesMetadata, prefix, fieldAnn);
            }
        }

        private void InitializeMembers(
            System.Type clazz, PropertiesMetadata propertiesMetadata, bool isRoot, String prefix,
            ISet<System.Type> processedClasses)
        {
            List<ClassBridgeAttribute> classBridgeAnn = AttributeUtil.GetClassBridges(clazz);
            if (classBridgeAnn != null)
            {
                // Ok, pick up the parameters as well
                AttributeUtil.GetClassBridgeParameters(clazz, classBridgeAnn);

                // Now we can process the class bridges
                foreach (ClassBridgeAttribute cb in classBridgeAnn)
                    BindClassAnnotation(prefix, propertiesMetadata, cb);
            }

            PropertyInfo[] propertyInfos = clazz.GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
                InitializeMember(propertyInfo, propertiesMetadata, isRoot, prefix, processedClasses);

            FieldInfo[] fields = clazz.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo fieldInfo in fields)
                InitializeMember(fieldInfo, propertiesMetadata, isRoot, prefix, processedClasses);
        }

        /*
		private void processContainedIn(Object instance, List<LuceneWork> queue, PropertiesMetadata metadata, SearchFactory searchFactory)
		{
			not supported
		}
	    */

        private void ProcessContainedInValue(object value, List<LuceneWork> queue, System.Type valueClass,
                                             DocumentBuilder builder, SearchFactory searchFactory)
        {
            object id = GetMemberValue(value, builder.idGetter);
            builder.AddToWorkQueue(value, id, WorkType.Update, queue, searchFactory);
        }

        private static void SetAccessible(MemberInfo member)
        {
            // NB Not sure we need to do anything for C#
        }

        #endregion

        #region Public methods

        /// <summary>
        /// This add the new work to the queue, so it can be processed in a batch fashion later
        /// </summary>
        public void AddToWorkQueue(object entity, object id, WorkType workType, List<LuceneWork> queue,
                                   SearchFactory searchFactory)
        {
            System.Type entityClass = NHibernateUtil.GetClass(entity);
            foreach (LuceneWork luceneWork in queue)
                if (luceneWork.EntityClass == entityClass && luceneWork.Id.Equals(id))
                    return;
            // bool searchForContainers = false;
            string idString = idBridge.ObjectToString(id);

            switch (workType)
            {
                case WorkType.Add:
                    queue.Add(new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id)));
                    // searchForContainers = true;
                    break;

                case WorkType.Delete:
                case WorkType.Purge:
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    break;

                case WorkType.PurgeAll:
                    queue.Add(new PurgeAllLuceneWork(entityClass));
                    break;

                case WorkType.Update:
                    /**
                     * even with Lucene 2.1, use of indexWriter to update is not an option
                     * We can only delete by term, and the index doesn't have a term that
                     * uniquely identify the entry.
                     * But essentially the optimization we are doing is the same Lucene is doing, the only extra cost is the
                     * double file opening.
                    */
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    queue.Add(new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id)));
                    // searchForContainers = true;
                    break;

                case WorkType.Index:
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    LuceneWork work = new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id));
                    work.IsBatch = true;
                    queue.Add(work);
                    // searchForContainers = true;
                    break;

                default:
                    throw new AssertionFailure("Unknown WorkType: " + workType);
            }

            /**
		     * When references are changed, either null or another one, we expect dirty checking to be triggered (both sides
		     * have to be updated)
		     * When the internal object is changed, we apply the {Add|Update}Work on containedIns
		    */
            /*
		    if (searchForContainers)
			    processContainedIn(entity, queue, rootPropertiesMetadata, searchFactory);
		    */
        }

        public Document GetDocument(object instance, object id)
        {
            Document doc = new Document();
            System.Type instanceClass = instance.GetType();
            if (rootPropertiesMetadata.boost != null)
                doc.SetBoost(rootPropertiesMetadata.boost.Value);
            // TODO: Check if that should be an else?
            {
                Field classField =
                    new Field(CLASS_FIELDNAME, instanceClass.AssemblyQualifiedName, Field.Store.YES,
                              Field.Index.UN_TOKENIZED);
                doc.Add(classField);
                idBridge.Set(idKeywordName, id, doc, Field.Store.YES, Field.Index.UN_TOKENIZED, idBoost);
            }
            BuildDocumentFields(instance, doc, rootPropertiesMetadata);
            return doc;
        }

        private static void BuildDocumentFields(Object instance, Document doc, PropertiesMetadata propertiesMetadata)
        {
            if (instance == null) return;

            for (int i = 0; i < propertiesMetadata.fieldNames.Count; i++)
            {
                MemberInfo member = propertiesMetadata.fieldGetters[i];
                Object value = GetMemberValue(instance, member);
                propertiesMetadata.fieldBridges[i].Set(
                    propertiesMetadata.fieldNames[i], value, doc, propertiesMetadata.fieldStore[i],
                    propertiesMetadata.fieldIndex[i], GetBoost(member)
                    );
            }

            for (int i = 0; i < propertiesMetadata.embeddedGetters.Count; i++)
            {
                MemberInfo member = propertiesMetadata.embeddedGetters[i];
                Object value = GetMemberValue(instance, member);
                //if ( ! Hibernate.isInitialized( value ) ) continue; //this sounds like a bad idea 
                //TODO handle boost at embedded level: already stored in propertiesMedatada.boost
                BuildDocumentFields(value, doc, propertiesMetadata.embeddedPropertiesMetadata[i]);
            }
        }

        public Term GetTerm(object id)
        {
            return new Term(idKeywordName, idBridge.ObjectToString(id));
        }

        public String getIdKeywordName()
        {
            return idKeywordName;
        }

        public static System.Type GetDocumentClass(Document document)
        {
            String className = document.Get(CLASS_FIELDNAME);
            try
            {
                return ReflectHelper.ClassForName(className);
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to load indexed class: " + className, e);
            }
        }

        public static object GetDocumentId(SearchFactory searchFactory, Document document)
        {
            System.Type clazz = GetDocumentClass(document);
            DocumentBuilder builder = searchFactory.DocumentBuilders[clazz];
            if (builder == null) throw new SearchException("No Lucene configuration set up for: " + clazz.Name);
            return builder.IdBridge.Get(builder.getIdKeywordName(), document);
        }

        public void PostInitialize(ISet<System.Type> indexedClasses)
        {
            //this method does not requires synchronization
            System.Type plainClass = beanClass;
            ISet<System.Type> tempMappedSubclasses = new HashedSet<System.Type>();
            //together with the caller this creates a o(2), but I think it's still faster than create the up hierarchy for each class
            foreach (System.Type currentClass in indexedClasses)
                if (plainClass.IsAssignableFrom(currentClass))
                    tempMappedSubclasses.Add(currentClass);
            mappedSubclasses = tempMappedSubclasses;
        }

        #endregion
    }
}