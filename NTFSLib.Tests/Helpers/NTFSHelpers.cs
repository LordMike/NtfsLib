using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTFSLib.IO;
using NTFSLib.NTFS;

namespace NTFSLib.Tests.Helpers
{
    public static class NTFSHelpers
    {
        public static NtfsFile OpenFile(NtfsDirectory dir, string file)
        {
            NtfsFile currFile = dir.ListFiles(false).SingleOrDefault(s => s.Name.Equals(file, StringComparison.InvariantCultureIgnoreCase));

            Assert.IsNotNull(currFile);

            return currFile;
        }

        public static NtfsDirectory OpenDir(NTFSWrapper ntfsWrapper, string path)
        {
            Assert.IsTrue(Path.IsPathRooted(path));

            string[] dirs = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            NtfsDirectory currDir = ntfsWrapper.GetRootDirectory();

            foreach (string dir in dirs.Skip(1))        // Skip root (C:\)
            {
                IEnumerable<NtfsDirectory> subDirs = currDir.ListDirectories(false);
                NtfsDirectory subDir = subDirs.FirstOrDefault(s => s.Name.Equals(dir, StringComparison.InvariantCultureIgnoreCase));

                Assert.IsNotNull(subDir);

                currDir = subDir;
            }

            return currDir;
        }
    }
}