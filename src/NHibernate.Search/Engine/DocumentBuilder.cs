using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
using NHibernate.Search.Store;
using NHibernate.Search.Util;
using NHibernate.Util;
using FieldInfo=System.Reflection.FieldInfo;

namespace NHibernate.Search.Engine
{
    using Type=System.Type;

    /// <summary>
    /// Set up and provide a manager for indexes classes
    /// </summary>
    public class DocumentBuilder
    {
        public const string CLASS_FIELDNAME = "_hibernate_class";
        private static readonly ILog logger = LogManager.GetLogger(typeof(DocumentBuilder));

        private readonly PropertiesMetadata rootPropertiesMetadata;
        private readonly System.Type beanClass;
        private readonly IDirectoryProvider[] directoryProviders;
        private readonly IIndexShardingStrategy shardingStrategy;
        private String idKeywordName;
        private MemberInfo idGetter;
        private float? idBoost;
        private ITwoWayFieldBridge idBridge;
        private ISet<Type> mappedSubclasses = new HashedSet<System.Type>();
        private int level;
        private int maxLevel = int.MaxValue;
        private readonly ScopedAnalyzer analyzer;

        #region Nested type: PropertiesMetadata

        private enum Container
        {
            Object,
            Collection,
            Map,
            Array
        }

        private class PropertiesMetadata
        {
            public readonly List<float> classBoosts = new List<float>();
            public readonly List<IFieldBridge> classBridges = new List<IFieldBridge>();
            public readonly List<Field.Index> classIndexes = new List<Field.Index>();
            public readonly List<string> classNames = new List<string>();
            public readonly List<Field.Store> classStores = new List<Field.Store>();
            public readonly List<MemberInfo> containedInGetters = new List<MemberInfo>();
            public readonly List<Container> embeddedContainers = new List<Container>();
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

        public DocumentBuilder(System.Type clazz, Analyzer defaultAnalyzer, IDirectoryProvider[] directoryProviders,
                               IIndexShardingStrategy shardingStrategy)
        {
            analyzer = new ScopedAnalyzer();
            beanClass = clazz;
            this.directoryProviders = directoryProviders;
            this.shardingStrategy = shardingStrategy;

            if (clazz == null) throw new AssertionFailure("Unable to build a DocumemntBuilder with a null class");

            rootPropertiesMetadata = new PropertiesMetadata();
            rootPropertiesMetadata.boost = GetBoost(clazz);
            rootPropertiesMetadata.analyzer = defaultAnalyzer;

            Set<System.Type> processedClasses = new HashedSet<System.Type>();
            processedClasses.Add(clazz);
            InitializeMembers(clazz, rootPropertiesMetadata, true, string.Empty, processedClasses);
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

        public IDirectoryProvider[] DirectoryProviders
        {
            get { return directoryProviders; }
        }

        public IIndexShardingStrategy DirectoryProvidersSelectionStrategy
        {
            get { return shardingStrategy; }
        }

        public ITwoWayFieldBridge IdBridge
        {
            get { return idBridge; }
        }

        public ISet<System.Type> MappedSubclasses
        {
            get { return mappedSubclasses; }
        }

        public string IdentifierName
        {
            get { return idGetter.Name; }
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

            // Field > property > entity analyzer
            Analyzer localAnalyzer = (GetAnalyzer(fieldAnn.Analyzer) ?? GetAnalyzer(member)) ??
                                     propertiesMetadata.analyzer;
            if (localAnalyzer == null)
                throw new NotSupportedException("Analyzer should not be undefined");

            analyzer.AddScopedAnalyzer(fieldName, localAnalyzer);
        }

        private static void BuildDocumentFields(Object instance, Document doc, PropertiesMetadata propertiesMetadata)
        {
            if (instance == null) return;

            object unproxiedInstance = Unproxy(instance);

            for (int i = 0; i < propertiesMetadata.classBridges.Count; i++)
            {
                IFieldBridge fb = propertiesMetadata.classBridges[i];

                try
                {
                    fb.Set(propertiesMetadata.classNames[i],
                           unproxiedInstance,
                           doc,
                           propertiesMetadata.classStores[i],
                           propertiesMetadata.classIndexes[i],
                           propertiesMetadata.classBoosts[i]);
                }
                catch (Exception e)
                {
                    logger.Error(
                        string.Format(CultureInfo.InvariantCulture, "Error processing class bridge for {0}",
                                      propertiesMetadata.classNames[i]), e);
                }
            }

            for (int i = 0; i < propertiesMetadata.fieldNames.Count; i++)
            {
                try
                {
                    MemberInfo member = propertiesMetadata.fieldGetters[i];
                    Object value = GetMemberValue(unproxiedInstance, member);
                    propertiesMetadata.fieldBridges[i].Set(
                        propertiesMetadata.fieldNames[i],
                        value,
                        doc,
                        propertiesMetadata.fieldStore[i],
                        propertiesMetadata.fieldIndex[i],
                        GetBoost(member));
                }
                catch (Exception e)
                {
                    logger.Error(
                        string.Format(CultureInfo.InvariantCulture, "Error processing field bridge for {0}.{1}",
                                      unproxiedInstance.GetType().FullName, propertiesMetadata.fieldNames[i]), e);
                }
            }

            for (int i = 0; i < propertiesMetadata.embeddedGetters.Count; i++)
            {
                MemberInfo member = propertiesMetadata.embeddedGetters[i];
                Object value = GetMemberValue(unproxiedInstance, member);
                //TODO handle boost at embedded level: already stored in propertiesMedatada.boost

                if (value == null) continue;
                PropertiesMetadata embeddedMetadata = propertiesMetadata.embeddedPropertiesMetadata[i];
                try
                {
                    switch (propertiesMetadata.embeddedContainers[i])
                    {
                        case Container.Array:
                            foreach (object arrayValue in value as Array)
                                BuildDocumentFields(arrayValue, doc, embeddedMetadata);
                            break;

                        case Container.Collection:
                            // Need to cast down to IEnumerable to support ISet 
                            foreach (object collectionValue in value as IEnumerable)
                                BuildDocumentFields(collectionValue, doc, embeddedMetadata);
                            break;

                        case Container.Map:
                            foreach (object collectionValue in (value as IDictionary).Values)
                                BuildDocumentFields(collectionValue, doc, embeddedMetadata);
                            break;

                        case Container.Object:
                            BuildDocumentFields(value, doc, embeddedMetadata);
                            break;

                        default:
                            throw new NotSupportedException("Unknown embedded container: " +
                                                            propertiesMetadata.embeddedContainers[i]);
                    }
                }
                catch (NullReferenceException)
                {
                    logger.Error(string.Format("Null reference whilst processing {0}.{1}, container type {2}",
                                               instance.GetType().FullName,
                                               member.Name, propertiesMetadata.embeddedContainers[i]));
                }
            }
        }

        private static string BuildEmbeddedPrefix(string prefix, IndexedEmbeddedAttribute embeddedAnn, MemberInfo member)
        {
            string localPrefix = prefix;
            if (embeddedAnn.Prefix == ".")
                // Default to property name
                localPrefix += member.Name + ".";
            else
                localPrefix += embeddedAnn.Prefix;

            return localPrefix;
        }

        private static Analyzer GetAnalyzer(ICustomAttributeProvider member)
        {
            AnalyzerAttribute attrib = AttributeUtil.GetAttribute<AnalyzerAttribute>(member);
            return attrib == null ? null : GetAnalyzer(attrib.Type);
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

        private static float? GetBoost(ICustomAttributeProvider element)
        {
            if (element == null) return null;
            BoostAttribute boost = AttributeUtil.GetAttribute<BoostAttribute>(element);
            if (boost == null)
                return null;
            return boost.Value;
        }

        private static int GetFieldPosition(string[] fields, string fieldName)
        {
            int fieldNbr = fields.GetUpperBound(0);
            for (int index = 0; index < fieldNbr; index++)
            {
                if (fieldName.Equals(fields[index])) return index;
            }
            return -1;
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

        private static object GetMemberValue(Object instance, MemberInfo getter)
        {
            PropertyInfo info = getter as PropertyInfo;
            return info != null ? info.GetValue(instance, null) : ((FieldInfo) getter).GetValue(instance);
        }

        private static System.Type GetMemberTypeOrGenericArguments(MemberInfo member)
        {
            Type type = GetMemberType(member);
            if (type.IsGenericType)
            {
                Type[] arguments = type.GetGenericArguments();
                //if we have more than one generic arg, we assume that this is a map
                // and return its value
                return arguments[arguments.Length - 1];
            }
            return type;
        }

        private static Type GetMemberTypeOrGenericCollectionType(MemberInfo member)
        {
            Type type = GetMemberType(member);
            return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }


        private static Type GetMemberType(MemberInfo member)
        {
            PropertyInfo info = member as PropertyInfo;
            return info != null ? info.PropertyType : ((FieldInfo) member).FieldType;
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
                            "Bridge for document id does not implement TwoWayFieldBridge: " + member.Name);
                    idBoost = GetBoost(member);
                    SetAccessible(member);
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

                    // Property > entity analyzer - no field analyzer
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

            IndexedEmbeddedAttribute embeddedAttribute = AttributeUtil.GetAttribute<IndexedEmbeddedAttribute>(member);
            if (embeddedAttribute != null)
            {
                int oldMaxLevel = maxLevel;
                int potentialLevel = embeddedAttribute.Depth + level;
                if (potentialLevel < 0)
                    potentialLevel = int.MaxValue;

                maxLevel = potentialLevel > maxLevel ? maxLevel : potentialLevel;
                level++;

                System.Type elementType = embeddedAttribute.TargetElement ?? GetMemberTypeOrGenericArguments(member);

                if (maxLevel == int.MaxValue && processedClasses.Contains(elementType))
                    throw new SearchException(
                        string.Format("Circular reference, Duplicate use of {0} in root entity {1}#{2}",
                                      elementType.FullName, beanClass.FullName,
                                      BuildEmbeddedPrefix(prefix, embeddedAttribute, member)));

                if (level <= maxLevel)
                {
                    processedClasses.Add(elementType); // push

                    SetAccessible(member);
                    propertiesMetadata.embeddedGetters.Add(member);
                    PropertiesMetadata metadata = new PropertiesMetadata();
                    propertiesMetadata.embeddedPropertiesMetadata.Add(metadata);
                    metadata.boost = GetBoost(member);
                    // property > entity analyzer
                    metadata.analyzer = GetAnalyzer(member) ?? propertiesMetadata.analyzer;
                    string localPrefix = BuildEmbeddedPrefix(prefix, embeddedAttribute, member);
                    InitializeMembers(elementType, metadata, false, localPrefix, processedClasses);

                    /**
                     * We will only index the "expected" type but that's OK, HQL cannot do downcasting either
                     */
                    // ayende: because we have to deal with generic collections here, we aren't 
                    // actually using the element type to determain what the value is, since that 
                    // was resolved to the element type of the possible collection
                    Type actualFieldType = GetMemberTypeOrGenericCollectionType(member);
                    if (actualFieldType.IsArray)
                        propertiesMetadata.embeddedContainers.Add(Container.Array);
                    else if (typeof(IDictionary).IsAssignableFrom(actualFieldType) ||
                        typeof(IDictionary<,>).IsAssignableFrom(actualFieldType))
                        propertiesMetadata.embeddedContainers.Add(Container.Map);
                    else if (typeof(ICollection).IsAssignableFrom(actualFieldType))
                        propertiesMetadata.embeddedContainers.Add(Container.Collection);
                    else if (typeof(IEnumerable).IsAssignableFrom(actualFieldType))
                        // NB We only see ISet as IEnumerable
                        propertiesMetadata.embeddedContainers.Add(Container.Collection);
                    else
                        propertiesMetadata.embeddedContainers.Add(Container.Object);

                    processedClasses.Remove(actualFieldType); // pop
                }
                else if (logger.IsDebugEnabled)
                {
                    string localPrefix = BuildEmbeddedPrefix(prefix, embeddedAttribute, member);
                    logger.Debug("Depth reached, ignoring " + localPrefix);
                }

                level--;
                maxLevel = oldMaxLevel; // set back the old max level
            }

            ContainedInAttribute containedInAttribute = AttributeUtil.GetAttribute<ContainedInAttribute>(member);
            if (containedInAttribute != null)
            {
                SetAccessible(member);
                propertiesMetadata.containedInGetters.Add(member);
            }
        }

        private void InitializeMembers(
            System.Type clazz, PropertiesMetadata propertiesMetadata, bool isRoot, String prefix,
            ISet<System.Type> processedClasses)
        {
            IList<System.Type> hierarchy = new List<System.Type>();
            System.Type currClass = clazz;
            do
            {
                hierarchy.Add(currClass);
                currClass = currClass.BaseType;
                // NB Java stops at null we stop at object otherwise we process the class twice
                // We also need a null test for things like ISet which have no base class/interface
            } while (currClass != null && currClass != typeof(object));

            for (int index = hierarchy.Count - 1; index >= 0; index--)
            {
                currClass = hierarchy[index];
                /**
                 * Override the default analyzer for the properties if the class hold one
                 * That's the reason we go down the hierarchy
                 */

                // NB Must cast here as we want to look at the type's metadata
                Analyzer localAnalyzer = GetAnalyzer(currClass as MemberInfo);
                if (localAnalyzer != null)
                    propertiesMetadata.analyzer = localAnalyzer;

                // Check for any ClassBridges
                List<ClassBridgeAttribute> classBridgeAnn = AttributeUtil.GetClassBridges(currClass);
                if (classBridgeAnn != null)
                {
                    // Ok, pick up the parameters as well
                    AttributeUtil.GetClassBridgeParameters(currClass, classBridgeAnn);

                    // Now we can process the class bridges
                    foreach (ClassBridgeAttribute cb in classBridgeAnn)
                        BindClassAnnotation(prefix, propertiesMetadata, cb);
                }

                // NB As we are walking the hierarchy only retrieve items at this level
                PropertyInfo[] propertyInfos =
                    currClass.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public |
                                            BindingFlags.Instance);
                foreach (PropertyInfo propertyInfo in propertyInfos)
                    InitializeMember(propertyInfo, propertiesMetadata, isRoot, prefix, processedClasses);

                FieldInfo[] fields =
                    clazz.GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public |
                                    BindingFlags.Instance);
                foreach (FieldInfo fieldInfo in fields)
                    InitializeMember(fieldInfo, propertiesMetadata, isRoot, prefix, processedClasses);
            }
        }


        private static void ProcessFieldsForProjection(PropertiesMetadata metadata, String[] fields, Object[] result,
                                                       Document document)
        {
            int nbrFoEntityFields = metadata.fieldNames.Count;
            for (int index = 0; index < nbrFoEntityFields; index++)
            {
                PopulateResult(metadata.fieldNames[index],
                               metadata.fieldBridges[index],
                               metadata.fieldStore[index],
                               fields,
                               result,
                               document
                    );
            }
            int nbrOfEmbeddedObjects = metadata.embeddedPropertiesMetadata.Count;
            for (int index = 0; index < nbrOfEmbeddedObjects; index++)
            {
                //there is nothing we can do for collections
                if (metadata.embeddedContainers[index] == Container.Object)
                {
                    ProcessFieldsForProjection(metadata.embeddedPropertiesMetadata[index], fields, result, document);
                }
            }
        }

        private static void PopulateResult(string fieldName, IFieldBridge fieldBridge, Field.Store store,
                                           string[] fields, object[] result, Document document)
        {
            int matchingPosition = GetFieldPosition(fields, fieldName);
            if (matchingPosition != -1)
            {
                //TODO make use of an isTwoWay() method
                if (store != Field.Store.NO && typeof(ITwoWayFieldBridge).IsAssignableFrom(fieldBridge.GetType()))
                {
                    result[matchingPosition] = ((ITwoWayFieldBridge) fieldBridge).Get(fieldName, document);
                    if (logger.IsInfoEnabled)
                    {
                        logger.Info("Field " + fieldName + " projected as " + result[matchingPosition]);
                    }
                }
                else
                {
                    if (store == Field.Store.NO)
                    {
                        throw new SearchException("Projecting an unstored field: " + fieldName);
                    }
                    
                    throw new SearchException("IFieldBridge is not a ITwoWayFieldBridge: " + fieldBridge.GetType());
                }
            }
        }

        private static void ProcessContainedIn(Object instance, List<LuceneWork> queue, PropertiesMetadata metadata,
                                               ISearchFactoryImplementor searchFactoryImplementor)
        {
            for (int i = 0; i < metadata.containedInGetters.Count; i++)
            {
                MemberInfo member = metadata.containedInGetters[i];
                object value = GetMemberValue(instance, member);

                if (value == null) continue;

                Array array = value as Array;
                if (array != null)
                {
                    foreach (object arrayValue in array)
                    {
                        // Highly inneficient but safe wrt the actual targeted class, e.g. polymorphic items in the array
                        System.Type valueType = NHibernateUtil.GetClass(arrayValue);
                        if (valueType == null || !searchFactoryImplementor.DocumentBuilders.ContainsKey(valueType))
                            continue;

                        ProcessContainedInValue(arrayValue, queue, valueType, searchFactoryImplementor.DocumentBuilders[valueType],
                                                searchFactoryImplementor);
                    }
                }
                else if (typeof(IEnumerable).IsAssignableFrom(value.GetType()))
                {
                    // NB We only see ISet and IDictionary`2 as IEnumerable
                    IEnumerable collection = value as IEnumerable;
                    if (typeof(IDictionary).IsAssignableFrom(value.GetType()))
                        collection = ((IDictionary) value).Values;

                    if (collection == null)
                        continue;

                    foreach (object collectionValue in collection)
                    {
                        // Highly inneficient but safe wrt the actual targeted class, e.g. polymorphic items in the array
                        System.Type valueType = NHibernateUtil.GetClass(collectionValue);
                        if (valueType == null || !searchFactoryImplementor.DocumentBuilders.ContainsKey(valueType))
                            continue;

                        ProcessContainedInValue(collectionValue, queue, valueType,
                                                searchFactoryImplementor.DocumentBuilders[valueType], searchFactoryImplementor);
                    }
                }
                else
                {
                    System.Type valueType = NHibernateUtil.GetClass(value);
                    if (valueType == null || !searchFactoryImplementor.DocumentBuilders.ContainsKey(valueType))
                        continue;

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
            object id = GetMemberValue(value, builder.idGetter);
            builder.AddToWorkQueue(valueClass, value, id, WorkType.Update, queue, searchFactory);
        }

        private static void SetAccessible(MemberInfo member)
        {
            // NB Not sure we need to do anything for C#
        }

        private static object Unproxy(object value)
        {
            // NB Not sure if we need to do anything for C#
            //return NHibernateUtil.Unproxy(value);
            return value;
        }

        #endregion

        #region Public methods

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
            string idString = idBridge.ObjectToString(id);

            switch (workType)
            {
                case WorkType.Add:
                    queue.Add(new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id)));
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
                    queue.Add(new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id)));
                    searchForContainers = true;
                    break;

                case WorkType.Index:
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    LuceneWork work = new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id));
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
                ProcessContainedIn(entity, queue, rootPropertiesMetadata, searchFactoryImplementor);
            }
        }

        public Document GetDocument(object instance, object id)
        {
            Document doc = new Document();
            System.Type instanceClass = instance.GetType();
            if (rootPropertiesMetadata.boost != null)
            {
                doc.SetBoost(rootPropertiesMetadata.boost.Value);
            }

            // TODO: Check if that should be an else?
            {
                Field classField = new Field(CLASS_FIELDNAME, TypeHelper.LuceneTypeName(instanceClass), Field.Store.YES, Field.Index.UN_TOKENIZED);
                doc.Add(classField);
                idBridge.Set(idKeywordName, id, doc, Field.Store.YES, Field.Index.UN_TOKENIZED, idBoost);
            }

            BuildDocumentFields(instance, doc, rootPropertiesMetadata);
            return doc;
        }

        public Term GetTerm(object id)
        {
            return new Term(idKeywordName, idBridge.ObjectToString(id));
        }

        public String GetIdKeywordName()
        {
            return idKeywordName;
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
            if (builder.idKeywordName != null)
            {
                PopulateResult(builder.idKeywordName, builder.idBridge, Field.Store.YES, fields, result, document);
            }

            PropertiesMetadata metadata = builder.rootPropertiesMetadata;
            ProcessFieldsForProjection(metadata, fields, result, document);

            return result;
        }

        public void PostInitialize(ISet<System.Type> indexedClasses)
        {
            // this method does not requires synchronization
            Type plainClass = beanClass;
            ISet<Type> tempMappedSubclasses = new HashedSet<System.Type>();

            // together with the caller this creates a o(2), but I think it's still faster than create the up hierarchy for each class
            foreach (Type currentClass in indexedClasses)
            {
                if (plainClass.IsAssignableFrom(currentClass))
                {
                    tempMappedSubclasses.Add(currentClass);
                }
            }

            mappedSubclasses = tempMappedSubclasses;
        }

        #endregion
    }
}
