using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using NHibernate.Cfg;
using NHibernate.Test;
using NHibernate.Tool.hbm2ddl;

using NUnit.Framework;

namespace NHibernate.Search.Tests.DirectoryProvider
{
    /// <summary>
    /// The multiply session factories test case.
    /// </summary>
    public abstract class MultiplySessionFactoriesTestCase
    {
        private readonly List<ISessionFactory> sessionFactories = new List<ISessionFactory>();
        private List<Configuration> configurations;

        /// <summary>
        /// Mapping files used in the TestCase
        /// </summary>
        protected abstract IList Mappings { get; }

        protected abstract int NumberOfSessionFactories { get; }

        #region Constructors
        #endregion

        #region Property methods

        /// <summary>
        /// Gets SessionFactories.
        /// </summary>
        protected IList<ISessionFactory> SessionFactories
        {
            get { return this.sessionFactories; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// The fixture set up.
        /// </summary>
        [OneTimeSetUp]
        public virtual void FixtureSetUp()
        {
            Configure();
            CreateSchema();
            BuildSessionFactories();
        }

        /// <summary>
        /// The build session factories.
        /// </summary>
        public void BuildSessionFactories()
        {
            foreach (Configuration configuration in configurations)
            {
                ISessionFactory sessionFactory = configuration.BuildSessionFactory();
                sessionFactories.Add(sessionFactory);
            }
        }

        #endregion

        /// <summary>
        /// The configure.
        /// </summary>
        /// <param name="cfg">
        /// The cfg.
        /// </param>
        protected abstract void Configure(IList<Configuration> cfg);

        /// <summary>
        /// The create schema.
        /// </summary>
        private void CreateSchema()
        {
            foreach (Configuration configuration in configurations)
            {
                new SchemaExport(configuration).Create(false, true);
            }
        }

        /// <summary>
        /// The configure.
        /// </summary>
        private void Configure()
        {
            configurations = new List<Configuration>();
            for (int i = 0; i < NumberOfSessionFactories; i++)
            {
                configurations.Add(CreateConfiguration());
            }

            Configure(configurations);
        }

        /// <summary>
        /// The create configuration.
        /// </summary>
        /// <returns>
        /// </returns>
        private Configuration CreateConfiguration()
        {
            Configuration cfg = new Configuration();
            if (TestConfigurationHelper.hibernateConfigFile != null)
            {
                cfg.Configure(TestConfigurationHelper.hibernateConfigFile);
            }

            Assembly assembly = Assembly.GetExecutingAssembly();

            foreach (string file in Mappings)
            {
                cfg.AddResource(assembly.GetName().Name + "." + file, assembly);
            }

            SearchTestCase.SetListener(cfg);
            return cfg;
        }
    }
}