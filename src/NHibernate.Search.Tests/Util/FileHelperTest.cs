using NUnit.Framework;

namespace NHibernate.Search.Tests.Util
{
    using System.IO;
    using System.Threading;

    using Store;

    [TestFixture]
    public class FileHelperTest
    {
        [Test]
        public void Synchronize()
        {
            DirectoryInfo src = new DirectoryInfo("./filehelpersrc");
            DirectoryInfo dest = new DirectoryInfo("./filehelperdest");
            FileHelper.Synchronize(src, dest, true);

            Assert.IsTrue(File.Exists("./filehelperdest/b"), "b copied");
            Assert.IsTrue(File.Exists("./filehelperdest/subdir/c"), "c copied");

            // change
            FileInfo test = CreateFile("./filehelpersrc", "c");
            Thread.Sleep(2000);
            FileInfo destTest = CreateFile("./filehelperdest", "c", false);
            Assert.AreNotEqual(test.Length, destTest.Length, "Lengths same");
            Assert.AreNotEqual(test.LastWriteTime, destTest.LastWriteTime, "Write times same");

            FileHelper.Synchronize(src, dest, true);
            // For .NET we have to refresh the object
            destTest.Refresh();
            Assert.AreEqual(test.Length, destTest.Length, "Lengths different");
            Assert.AreEqual(test.LastWriteTime, destTest.LastWriteTime, "Write times different");

            // delete
            test.Delete();
            FileHelper.Synchronize(src, dest, true);
            destTest.Refresh();
            Assert.IsTrue(!destTest.Exists, "dest c still exists");
        }

        #region Helper methods

        [SetUp]
        public void SetUp()
        {
            CreateFile("./filehelpersrc", "a");
            CreateFile("./filehelpersrc", "b");
            CreateFile("./filehelpersrc/subdir", "c");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists("./filehelpersrc"))
            {
                Directory.Delete("./filehelpersrc", true);
            }

            if (Directory.Exists("./filehelperdest"))
            {
                Directory.Delete("./filehelperdest", true);
            }
        }

        private FileInfo CreateFile(string directory, string name)
        {
            return CreateFile(directory, name, true);
        }

        private FileInfo CreateFile(string directory, string name, bool write)
        {
            DirectoryInfo di = new DirectoryInfo(directory);
            if (!di.Exists)
            {
                di.Create();
            }

            string fileName = di.FullName + "\\" + name;
            StreamWriter sw = File.CreateText(fileName);
            try
            {
                if (write)
                {
                    sw.Write(1);
                    sw.Write(2);
                    sw.Write(3);
                }
            }
            finally
            {
                sw.Close();
            }

            return new FileInfo(fileName);
        }

        #endregion
    }
}
