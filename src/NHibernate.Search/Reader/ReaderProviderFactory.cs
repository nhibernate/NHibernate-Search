using System;
using System.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Util;

namespace NHibernate.Search.Reader
{
    public static class ReaderProviderFactory
    {
        private static IDictionary<string, string> GetProperties(Configuration cfg)
        {
            IDictionary<string, string> workerProps = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> prop in cfg.Properties)
            {
                if (prop.Key.StartsWith(Environment.ReaderPrefix))
                    workerProps.Add(prop);
            }
            return workerProps;
        }

        public static IReaderProvider CreateReaderProvider(Configuration cfg, ISearchFactoryImplementor searchFactoryImplementor)
        {
            IDictionary<string, string> props = GetProperties(cfg);
            string impl = props.ContainsKey(Environment.ReaderStrategy) ? props[Environment.ReaderStrategy] : string.Empty;
            IReaderProvider readerProvider;
            if (string.IsNullOrEmpty(impl))
                // Put in another one
                readerProvider = new SharedReaderProvider();
            else if (impl.ToLowerInvariant() == "not-shared")
                readerProvider = new NotSharedReaderProvider();
            else if (impl.ToLowerInvariant() == "shared")
                readerProvider = new SharedReaderProvider();
            else
            {
                try
                {
                    readerProvider = (IReaderProvider) Activator.CreateInstance(ReflectHelper.ClassForName(impl));
                }
                catch (InvalidCastException)
                {
                    throw new SearchException(string.Format("Class does not implement IReaderProvider: {0}", impl));
                }
                catch (Exception)
                {
                    throw new SearchException("Failed to instantiate IReaderProvider with type " + impl);
                }

            }
            readerProvider.Initialize(props, searchFactoryImplementor);

            return readerProvider;
        }
    }
}