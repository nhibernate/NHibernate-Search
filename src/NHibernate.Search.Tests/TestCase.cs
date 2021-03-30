using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Reflection;

using log4net;
using log4net.Config;

using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Type;

using NUnit.Framework;

namespace NHibernate.Test
{
    using System.IO;

    public abstract class TestCase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestCase));
        private const bool OutputDdl = false;

        protected Configuration cfg;
        protected ISessionFactory sessions;
        protected ISession lastOpenedSession;
        private DebugConnectionProvider connectionProvider;

        #region Property methods

        protected Dialect.Dialect Dialect
        {
            get { return NHibernate.Dialect.Dialect.GetDialect(cfg.Properties); }
        }

        /// <summary>
        /// Mapping files used in the TestCase
        /// </summary>
        protected abstract IList Mappings { get; }

        /// <summary>
        /// Assembly to load mapping files from (default is NHibernate.DomainModel).
        /// </summary>
        protected virtual string MappingsAssembly
        {
            get { return "NHibernate.DomainModel"; }
        }

        protected virtual string CacheConcurrencyStrategy
        {
            get { return "nonstrict-read-write"; }
        }

        protected ISessionFactoryImplementor Sfi
        {
            get { return (ISessionFactoryImplementor) sessions; }
        }

        protected virtual bool RunFixtureSetUpAndTearDownForEachTest
        {
            get { return false; }
        }

        #endregion

        static TestCase()
        {
            // Configure log4net here since configuration through an attribute doesn't always work.
            XmlConfigurator.Configure();
        }

        #region Public methods

        /// <summary>
        /// Creates the tables used in this TestCase
        /// </summary>
        [SetUp]
        public void TestFixtureSetUp()
        {
            if (!RunFixtureSetUpAndTearDownForEachTest)
                TestFixtureSetUpInternal();
        }

        private void TestFixtureSetUpInternal()
        {
            try
            {
                Configure();
                if (!AppliesTo(Dialect))
                {
                    Assert.Ignore(GetType() + " does not apply to " + Dialect);
                }

                CreateSchema();
                BuildSessionFactory();
            }
            catch (Exception e)
            {
                log.Error("Error while setting up the test fixture", e);
                throw;
            }
        }

        /// <summary>
        /// Removes the tables used in this TestCase.
        /// </summary>
        /// <remarks>
        /// If the tables are not cleaned up sometimes SchemaExport runs into
        /// Sql errors because it can't drop tables because of the FKs.  This 
        /// will occur if the TestCase does not have the same hbm.xml files
        /// included as a previous one.
        /// </remarks>
        [TearDown]
        public void TestFixtureTearDown()
        {
            if (!RunFixtureSetUpAndTearDownForEachTest)
                TestFixtureTearDownInternal();
        }

        private void TestFixtureTearDownInternal()
        {
            DropSchema();
            Cleanup();
        }

        /// <summary>
        /// Set up the test. This method is not overridable, but it calls
        /// <see cref="OnSetUp" /> which is.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            if (RunFixtureSetUpAndTearDownForEachTest)
                TestFixtureSetUpInternal();

            OnSetUp();
        }

        /// <summary>
        /// Checks that the test case cleans up after itself. This method
        /// is not overridable, but it calls <see cref="OnTearDown" /> which is.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            OnTearDown();

            bool wasClosed = CheckSessionWasClosed();
            bool wasCleaned = CheckDatabaseWasCleaned();
            bool wereConnectionsClosed = CheckConnectionsWereClosed();
            bool fail = !wasClosed || !wasCleaned || !wereConnectionsClosed;

            if (fail)
            {
                Assert.Fail($"Test didn't clean up after itself - session closed: {wasClosed}; database cleaned: {wasCleaned}; connections closed: {wereConnectionsClosed}");
            }

            if (RunFixtureSetUpAndTearDownForEachTest)
                TestFixtureTearDownInternal();
        }

        #endregion

        #region Protected methods

        protected virtual void Configure(Configuration configuration)
        {
        }

        protected virtual void OnSetUp()
        {
        }

        protected virtual void OnTearDown()
        {
        }

        protected virtual bool AppliesTo(Dialect.Dialect dialect)
        {
            return true;
        }

        protected void Delete(FileInfo sub)
        {
            if (Directory.Exists(sub.FullName))
            {
                Directory.Delete(sub.FullName, true);
            }
            else
            {
                File.Delete(sub.FullName);
            }
        }

        protected virtual void BuildSessionFactory()
        {
            sessions = cfg.BuildSessionFactory();
            ISessionFactoryImplementor sessionsImpl = sessions as ISessionFactoryImplementor;
            connectionProvider = sessionsImpl == null
                                         ? null
                                         : sessionsImpl.ConnectionProvider as DebugConnectionProvider;
        }

        protected int ExecuteStatement(string sql)
        {
            if (cfg == null)
            {
                cfg = new Configuration();
            }

            using (IConnectionProvider prov = ConnectionProviderFactory.NewConnectionProvider(cfg.Properties))
            {
                DbConnection conn = prov.GetConnection();

                try
                {
                    using (IDbTransaction tran = conn.BeginTransaction())
                    {
                        using (IDbCommand comm = conn.CreateCommand())
                        {
                            comm.CommandText = sql;
                            comm.Transaction = tran;
                            comm.CommandType = CommandType.Text;
                            int result = comm.ExecuteNonQuery();
                            tran.Commit();
                            return result;
                        }
                    }
                }
                finally
                {
                    prov.CloseConnection(conn);
                }
            }
        }

        protected virtual ISession OpenSession()
        {
            ISession session = sessions.OpenSession();
            lastOpenedSession = session;

            // Don't return lastOpenedSession because it might have already been changed by another concurrent thread
            return session;
        }

        protected void ApplyCacheSettings(Configuration configuration)
        {
            if (CacheConcurrencyStrategy == null)
            {
                return;
            }

            foreach (PersistentClass clazz in configuration.ClassMappings)
            {
                bool hasLob = false;
                foreach (Property prop in clazz.PropertyClosureIterator)
                {
                    if (prop.Value.IsSimpleValue)
                    {
                        IType type = ((SimpleValue) prop.Value).Type;
                        if (type == NHibernateUtil.BinaryBlob)
                        {
                            hasLob = true;
                        }
                    }
                }

                if (!hasLob && !clazz.IsInherited)
                {
                    configuration.SetCacheConcurrencyStrategy(
                            clazz.MappedClass.AssemblyQualifiedName, CacheConcurrencyStrategy);
                }
            }

            /*foreach (Mapping.Collection coll in configuration.CollectionMappings)
            {
                configuration.SetCacheConcurrencyStrategy(coll.Role, CacheConcurrencyStrategy);
            }*/
        }

        #endregion

        #region Private methods

        private bool CheckSessionWasClosed()
        {
            if (lastOpenedSession != null && lastOpenedSession.IsOpen)
            {
                log.Error("Test case didn't close a session, closing");
                lastOpenedSession.Close();
                return false;
            }

            return true;
        }

        private bool CheckDatabaseWasCleaned()
        {
            if (sessions.GetAllClassMetadata().Count == 0)
            {
                // Return early in the case of no mappings, also avoiding
                // a warning when executing the HQL below.
                return true;
            }

            bool empty;
            using (ISession s = sessions.OpenSession())
            {
                IList objects = s.CreateQuery("from System.Object o").List();
                empty = objects.Count == 0;
            }

            if (!empty)
            {
                log.Error("Test case didn't clean up the database after itself, re-creating the schema");
                DropSchema();
                CreateSchema();
            }

            return empty;
        }

        private bool CheckConnectionsWereClosed()
        {
            if (connectionProvider == null || !connectionProvider.HasOpenConnections)
            {
                return true;
            }

            log.Error("Test case didn't close all open connections, closing");
            connectionProvider.CloseAllConnections();
            return false;
        }

        private void Configure()
        {
            cfg = new Configuration();
            if (TestConfigurationHelper.hibernateConfigFile != null)
            {
                cfg.Configure(TestConfigurationHelper.hibernateConfigFile);
            }

            Assembly assembly = Assembly.Load(MappingsAssembly);

            foreach (string file in Mappings)
            {
                cfg.AddResource(MappingsAssembly + "." + file, assembly);
            }

            Configure(cfg);

            ApplyCacheSettings(cfg);
        }

        private void CreateSchema()
        {
            new SchemaExport(cfg).Create(OutputDdl, true);
        }

        private void DropSchema()
        {
            new SchemaExport(cfg).Drop(OutputDdl, true);
        }

        private void Cleanup()
        {
            sessions.Close();
            sessions = null;
            connectionProvider = null;
            lastOpenedSession = null;
            cfg = null;
        }

        #endregion
    }
}