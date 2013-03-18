using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NTFSLib.Helpers;
using NTFSLib.IO;
using NTFSLib.NTFS;
using NTFSLib.Tests.Helpers;
using RawDiskLib;

namespace NTFSLib.Tests
{
    [TestClass]
    public class NTFSDirectoryTests
    {
        [TestMethod]
        public void EnumerateChilds()
        {
            Random rand = new Random();

            using (TempDir tmpDir = new TempDir())
            {
                // Make files
                byte[] data = new byte[1024 * 1024];
                for (int i = 0; i < 10; i++)
                {
                    rand.NextBytes(data);

                    File.WriteAllBytes(Path.Combine(tmpDir.Directory.FullName, i + ".bin"), data);
                }

                // Make dirs
                for (int i = 0; i < 10; i++)
                {
                    rand.NextBytes(data);

                    tmpDir.Directory.CreateSubdirectory("dir" + i);
                }

                // Discover dir in NTFSLib
                char driveLetter = tmpDir.Directory.FullName[0];
                RawDisk disk = new RawDisk(driveLetter);

                NTFSDiskProvider provider = new NTFSDiskProvider(disk);

                NTFSWrapper ntfsWrapper = new NTFSWrapper(provider, 0);

                NtfsDirectory ntfsDir = NTFSHelpers.OpenDir(ntfsWrapper, tmpDir.Directory.FullName);

                // Enumerate files
                List<NtfsFile> ntfsFiles = ntfsDir.ListFiles().ToList();

                Assert.AreEqual(10, ntfsFiles.Count);

                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(1, ntfsFiles.Count(s => s.Name == i + ".bin"));
                }

                // Enumerate dirs
                List<NtfsDirectory> ntfsDirs = ntfsDir.ListDirectories().ToList();

                Assert.AreEqual(10, ntfsDirs.Count);

                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(1, ntfsDirs.Count(s => s.Name == "dir" + i));
                }
            }
        }

        [TestMethod]
        public void AlternateDatastreamFile()
        {
            Random rand = new Random();

            byte[][] data = new byte[11][];
            for (int i = 0; i < 11; i++)
            {
                data[i] = new byte[1024 * 1024];
                rand.NextBytes(data[i]);
            }

            using (TempFile tmpFile = new TempFile())
            {
                // Make file
                File.WriteAllBytes(tmpFile.File.FullName, data[10]);

                for (int i = 0; i < 10; i++)
                {
                    using (SafeFileHandle fileHandle = Win32.CreateFile(tmpFile.File.FullName + ":alternate" + i + ":$DATA"))
                    using (FileStream fs = new FileStream(fileHandle, FileAccess.ReadWrite))
                    {
                        fs.Write(data[i], 0, data[i].Length);
                    }
                }

                // Discover file in NTFSLib
                char driveLetter = tmpFile.File.DirectoryName[0];
                RawDisk disk = new RawDisk(driveLetter);

                NTFSDiskProvider provider = new NTFSDiskProvider(disk);

                NTFSWrapper ntfsWrapper = new NTFSWrapper(provider, 0);

                NtfsDirectory ntfsDir = NTFSHelpers.OpenDir(ntfsWrapper, tmpFile.File.DirectoryName);
                NtfsFile ntfsFile = NTFSHelpers.OpenFile(ntfsDir, tmpFile.File.Name);

                // Check streams
                string[] streams = ntfsWrapper.ListDatastreams(ntfsFile.MFTRecord);

                Assert.AreEqual(11, streams.Length);
                Assert.AreEqual(1, streams.Count(s => s == string.Empty));

                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(1, streams.Count(s => s == "alternate" + i));
                }

                // Check data
                using (Stream memStream = new MemoryStream(data[10]))
                using (Stream fileStream = ntfsWrapper.OpenFileRecord(ntfsFile.MFTRecord))
                {
                    StreamUtils.CompareStreams(memStream, fileStream);
                }

                for (int i = 0; i < 10; i++)
                {
                    using (Stream memStream = new MemoryStream(data[i]))
                    using (Stream fileStream = ntfsWrapper.OpenFileRecord(ntfsFile.MFTRecord, "alternate" + i))
                    {
                        StreamUtils.CompareStreams(memStream, fileStream);
                    }
                }
            }
        }

        [TestMethod]
        public void AlternateDatastreamDirectory()
        {
            Random rand = new Random();

            byte[][] data = new byte[10][];
            for (int i = 0; i < 10; i++)
            {
                data[i] = new byte[1024 * 1024];
                rand.NextBytes(data[i]);
            }

            using (TempDir tmpDir = new TempDir())
            {
                // Make file
                for (int i = 0; i < 10; i++)
                {
                    using (SafeFileHandle fileHandle = Win32.CreateFile(tmpDir.Directory.FullName + ":alternate" + i + ":$DATA"))
                    using (FileStream fs = new FileStream(fileHandle, FileAccess.ReadWrite))
                    {
                        fs.Write(data[i], 0, data[i].Length);
                    }
                }

                // Discover dir in NTFSLib
                char driveLetter = tmpDir.Directory.FullName[0];
                RawDisk disk = new RawDisk(driveLetter);

                NTFSDiskProvider provider = new NTFSDiskProvider(disk);

                NTFSWrapper ntfsWrapper = new NTFSWrapper(provider, 0);

                NtfsDirectory ntfsDir = NTFSHelpers.OpenDir(ntfsWrapper, tmpDir.Directory.FullName);

                // Check streams
                string[] streams = ntfsWrapper.ListDatastreams(ntfsDir.MFTRecord);

                Assert.AreEqual(10, streams.Length);

                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(1, streams.Count(s => s == "alternate" + i));
                }

                // Check data
                for (int i = 0; i < 10; i++)
                {
                    using (Stream memStream = new MemoryStream(data[i]))
                    using (Stream fileStream = ntfsWrapper.OpenFileRecord(ntfsDir.MFTRecord, "alternate" + i))
                    {
                        StreamUtils.CompareStreams(memStream, fileStream);
                    }
                }
            }
        }
    }
}
