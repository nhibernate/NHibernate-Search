using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace NHibernate.Search.Tests.DirectoryProvider {
	using Test;

	public abstract class MultiplySessionFactoriesTestCase {
        private List<Configuration> configurations;
        private List<ISessionFactory> sessionFactories = new List<ISessionFactory>();

        protected abstract int NumberOfSessionFactories { get; }

        protected IList<ISessionFactory> SessionFactories {
            get { return sessionFactories; }
        }

        protected abstract IList Mappings { get; }

        [TestFixtureSetUp]
        public virtual void FixtureSetUp() {
            Configure();
            CreateSchema();
            BuildSessionFactories();
        }

        private void CreateSchema() {
            foreach (Configuration configuration in configurations)
                new SchemaExport(configuration).Create(false, true);
        }

        public void BuildSessionFactories() {
            foreach (Configuration configuration in configurations) {
                ISessionFactory sessionFactory = configuration.BuildSessionFactory();
                sessionFactories.Add(sessionFactory);
            }
        }

        private void Configure() {
            configurations = new List<Configuration>();
            for (int i = 0; i < NumberOfSessionFactories; i++)
                configurations.Add(CreateConfiguration());
            Configure(configurations);
        }

        private Configuration CreateConfiguration() {
            Configuration cfg = new Configuration();
			if (TestConfigurationHelper.hibernateConfigFile != null)
				cfg.Configure(TestConfigurationHelper.hibernateConfigFile);
            Assembly assembly = Assembly.GetExecutingAssembly();

            foreach (string file in Mappings)
                cfg.AddResource(assembly.GetName().Name + "." + file, assembly);
            SearchTestCase.SetListener(cfg);
            return cfg;
        }

        protected abstract void Configure(IList<Configuration> cfg);
    }
}