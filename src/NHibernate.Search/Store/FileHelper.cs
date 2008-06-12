using System.IO;
using Iesi.Collections.Generic;
using log4net;

namespace NHibernate.Search.Store
{
    public class FileHelper
    {
        private const int LastWriteTimePrecision = 2000;
        private static readonly ILog log = LogManager.GetLogger(typeof(FileHelper));

        public static void Synchronize(DirectoryInfo source, DirectoryInfo destination, bool smart)
        {
            if (!destination.Exists)
                destination.Create();
            FileInfo[] sources = source.GetFiles();
            ISet<string> srcNames = new HashedSet<string>();
            foreach (FileInfo fileInfo in sources)
                srcNames.Add(fileInfo.Name);
            FileInfo[] dests = destination.GetFiles();

            //delete files not present in source
            foreach (FileInfo file in dests)
                if (!srcNames.Contains(file.Name))
                    try
                    {
                        /*
                         * Try to delete, could fail because windows don't permit a file to be
                         * deleted while it is in use. If it is the case, the file would be deleted 
                         * when te index is reopened or in the next syncronization. 
                         */
                        file.Delete(); 
                    }
                    catch (System.IO.IOException e)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("Unable to delete file " + file.Name + ", maybe in use per another reader");
                    }
                   
            //copy each file from source
            foreach (FileInfo sourceFile in sources)
            {
                FileInfo destinationFile = new FileInfo(Path.Combine(destination.FullName, sourceFile.Name));
                long destinationChanged = destinationFile.LastWriteTime.Ticks/LastWriteTimePrecision;
                long sourceChanged = sourceFile.LastWriteTime.Ticks/LastWriteTimePrecision;
                if (!smart || destinationChanged != sourceChanged)
                    sourceFile.CopyTo(destinationFile.FullName, true);
            }

            foreach (DirectoryInfo directoryInfo in source.GetDirectories())
                Synchronize(directoryInfo,
                            new DirectoryInfo(Path.Combine(destination.FullName, directoryInfo.Name)),
                            smart);
        }
    }
}
