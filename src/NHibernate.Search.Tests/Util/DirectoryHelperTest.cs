using System.Collections.Generic;

namespace NHibernate.Search.Tests.Util
{
    using System.IO;
    using System.Threading;

    using NUnit.Framework;

    [TestFixture]
    public class DirectoryHelperTest
    {
        private const string root = @"./scratch";

        [Test]
        public void CreateViaRoot()
        {
            var properties = new Dictionary<string, string>();
            properties["sourceBase"] = root;

            DirectoryProviderHelper.GetSourceDirectory("sourceBase", "relativeBase", "Test", properties);

            DirectoryInfo info = new DirectoryInfo(root + "/Test");
            Assert.IsTrue(info.Exists);
        }

        [Test]
        public void CreateViaExistingRoot()
        {            
            var properties = new Dictionary<string, string>();
            properties["sourceBase"] = root;

            DirectoryInfo di = new DirectoryInfo(root);
            if (!di.Exists)
            {
                di.Create();
            }

            DirectoryProviderHelper.GetSourceDirectory("sourceBase", "relativeBase", "Test", properties);

            DirectoryInfo info = new DirectoryInfo(root + "/Test");
            Assert.IsTrue(info.Exists);
        }

        [Test]
        public void CreateViaRelative()
        {
            var properties = new Dictionary<string, string>();
            properties["sourceBase"] = root;
            properties["relativeBase"] = "./Fred";

            DirectoryProviderHelper.GetSourceDirectory("sourceBase", "relativeBase", "Test", properties);

            DirectoryInfo info = new DirectoryInfo(root + "/Fred");
            Assert.IsTrue(info.Exists);
        }

        #region Helper methods

        [SetUp]
        public void Setup()
        {
            ZapRoot();
        }

        [TearDown]
        public void TearDown()
        {
            ZapRoot();    
        }

        private void ZapRoot()
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
            catch (IOException)
            {
                // Wait for it to wind down for a while
                Thread.Sleep(1000);
            }
        }

        #endregion
    }
}