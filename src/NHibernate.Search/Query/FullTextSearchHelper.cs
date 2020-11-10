using System.Collections.Generic;
using Iesi.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NHibernate.Search.Engine;
using NHibernate.Search.Store;
using NHibernate.Search.Util;

namespace NHibernate.Search.Query
{
    public class FullTextSearchHelper
    {
        public static Lucene.Net.Search.Query FilterQueryByClasses(ISet<System.Type> classesAndSubclasses, Lucene.Net.Search.Query luceneQuery)
        {
            // A query filter is more practical than a manual class filtering post query (esp on scrollable resultsets)
            // it also probably minimise the memory footprint
            if (classesAndSubclasses == null)
            {
                return luceneQuery;
            }

            BooleanQuery classFilter = new BooleanQuery();

            // annihilate the scoring impact of DocumentBuilder.CLASS_FIELDNAME
            foreach (System.Type clazz in classesAndSubclasses)
            {
                Term t = new Term(DocumentBuilder.CLASS_FIELDNAME, TypeHelper.LuceneTypeName(clazz));
                TermQuery termQuery = new TermQuery(t);
                classFilter.Add(termQuery, Occur.SHOULD);
            }

            BooleanQuery filteredQuery = new BooleanQuery {{luceneQuery, Occur.MUST}, {classFilter, Occur.MUST}};
            return filteredQuery;
        }

        public static IndexSearcher BuildSearcher(ISearchFactoryImplementor searchFactory,
                                             out ISet<System.Type> classesAndSubclasses,
                                             params System.Type[] classes)
        {
            IDictionary<System.Type, DocumentBuilder> builders = searchFactory.DocumentBuilders;
            ISet<IDirectoryProvider> directories = new HashSet<IDirectoryProvider>();
            if (classes == null || classes.Length == 0)
            {
                // no class means all classes
                foreach (DocumentBuilder builder in builders.Values)
                {
                    foreach (IDirectoryProvider provider in builder.DirectoryProvidersSelectionStrategy.GetDirectoryProvidersForAllShards())
                    {
                        directories.Add(provider);
                    }
                }

                // Give them back an empty set
                classesAndSubclasses = null;
            }
            else
            {
                ISet<System.Type> involvedClasses = new HashSet<System.Type>();
                foreach (var c in classes)
                {
                    involvedClasses.Add(c);
                }
                foreach (System.Type clazz in classes)
                {
                    DocumentBuilder builder;
                    builders.TryGetValue(clazz, out builder);
                    if (builder != null)
                    {
                        foreach (var subClass in builder.MappedSubclasses)
                        {
                            involvedClasses.Add(subClass);
                        }
                    }
                }

                foreach (System.Type clazz in involvedClasses)
                {
                    DocumentBuilder builder;
                    builders.TryGetValue(clazz, out builder);

                    // TODO should we rather choose a polymorphic path and allow non mapped entities
                    if (builder == null)
                    {
                        throw new HibernateException("Not a mapped entity: " + clazz);
                    }

                    foreach (IDirectoryProvider provider in builder.DirectoryProvidersSelectionStrategy.GetDirectoryProvidersForAllShards())
                    {
                        directories.Add(provider);
                    }
                }

                classesAndSubclasses = involvedClasses;
            }

            IDirectoryProvider[] directoryProviders = new List<IDirectoryProvider>(directories).ToArray();
            return new IndexSearcher(searchFactory.ReaderProvider.OpenReader(directoryProviders));
        }
    }
}