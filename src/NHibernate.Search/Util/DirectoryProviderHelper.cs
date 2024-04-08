using System;
using System.Collections;
using System.IO;
using NHibernate.Search.Impl;
using NHibernate.Util;

namespace NHibernate.Search
{
    public class DirectoryProviderHelper
    {
        private static readonly INHibernateLogger log = NHibernateLogger.For(typeof(DirectoryProviderHelper));

        /// <summary>
        /// Build a directory name out of a root and relative path, guessing the significant part
        /// and checking for the file availability
        /// </summary>
        public static string GetSourceDirectory(string rootPropertyName, string relativePropertyName, string directoryProviderName, IDictionary properties)
        {
            // TODO check that it's a directory
            string root = (string)properties[rootPropertyName];
            string relative = (string)properties[relativePropertyName];
            if (log.IsDebugEnabled())
            {
                log.Debug(
                        "Guess source directory from " + rootPropertyName + " " + root != null
                                ? root
                                : "<null>" + " and " + relativePropertyName + " " + (relative ?? "<null>"));
            }

            if (relative == null)
            {
                relative = directoryProviderName;
            }

            if (StringHelper.IsEmpty(root))
            {
                log.Debug("No root directory, go with relative " + relative);
                DirectoryInfo sourceFile = new DirectoryInfo(relative);
                if (!sourceFile.Exists)
                {
                    throw new HibernateException("Unable to read source directory: " + relative);
                }
                //else keep source as it
            }
            else
            {
                DirectoryInfo rootDir = new DirectoryInfo(root);
                if (!rootDir.Exists)
                {
                    try
                    {
                        rootDir.Create();
                        rootDir = new DirectoryInfo(root);
                    }
                    catch (IOException e)
                    {
                        throw new SearchException(root + " does not exist and cannot be created", e);
                    }
                }

                // Test again in case Create failed for wrong reasons
                if (rootDir.Exists)
                {
                    DirectoryInfo sourceFile = new DirectoryInfo(Path.Combine(root, relative));
                    if (!sourceFile.Exists)
                    {
                        sourceFile.Create();
                    }

                    log.Debug("Get directory from root + relative");
                    try
                    {
                        relative = sourceFile.FullName;
                    }
                    catch (IOException)
                    {
                        throw new AssertionFailure("Unable to get canonical path: " + root + " + " + relative);
                    }
                }
                else
                {
                    throw new SearchException(root + " does not exist");
                }
            }

            return relative;
        }

        public static DirectoryInfo DetermineIndexDir(String directoryProviderName, IDictionary properties)
        {
            string indexBase = (string)properties["indexBase"];

            if (indexBase == null)
            {
                indexBase = ".";
            }
            else if (indexBase.StartsWith("~"))
            {
                // We need this to allow using the search from the web, where the "." directory is somewhere in the system root.
                indexBase = indexBase.Replace("~", AppDomain.CurrentDomain.BaseDirectory);
            }
            else if (indexBase.StartsWith(".."))
            {
                // determine the indexBase path when using parent directory (eg. "../indexes")

                DirectoryInfo targetParentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

                string path = indexBase;

                while (path.StartsWith(".."))
                {
                    if (targetParentDir.Parent == null)
                        throw new HibernateException("IndexBase path not valid");

                    targetParentDir = targetParentDir.Parent;
                    path = path.Remove(0, 3);
                }

                indexBase = Path.Combine(targetParentDir.FullName, path);
            }

            DirectoryInfo indexDir = new DirectoryInfo(indexBase);
            if (!indexDir.Exists)
            {
                // if the base directory does not exist, create it
                indexDir.Create();
            }

            if (!HasWriteAccess(indexDir))
            {
                throw new HibernateException("Cannot write into index directory: " + indexBase);
            }

            string indexName = (string)properties["indexName"] ?? directoryProviderName;

            return new DirectoryInfo(Path.Combine(indexDir.FullName, indexName));
        }

        private static bool HasWriteAccess(DirectoryInfo indexDir)
        {
            string tempFileName = Path.Combine(indexDir.FullName, Guid.NewGuid().ToString());

            // Yuck! but it is the simplest way
            try
            {
                File.CreateText(tempFileName).Close();
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            try
            {
                File.Delete(tempFileName);
            }
            catch (UnauthorizedAccessException)
            {
                // We may have permissions to create but not delete, ignoring
            }

            return true;
        }
    }
}