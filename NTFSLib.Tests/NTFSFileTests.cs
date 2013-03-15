using System;
using System.IO;
using System.Text;
using DeviceIOControlLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTFSLib.Helpers;
using NTFSLib.IO;
using NTFSLib.Objects.Attributes;
using NTFSLib.Tests.Helpers;
using RawDiskLib;
using System.Linq;
using FileAttributes = System.IO.FileAttributes;

namespace NTFSLib.Tests
{
    [TestClass]
    public class NTFSFileTests
    {
        private Random _random = new Random();

        [TestMethod]
        public void SimpleFile()
        {
            byte[] randomData = new byte[65 * 4096];
            _random.NextBytes(randomData);

            FileInfo tmpFile = new FileInfo(Path.GetTempFileName());
            try
            {
                // Create a file
                File.WriteAllBytes(tmpFile.FullName, randomData);

                // Discover it via the NTFS lib
                char driveLetter = tmpFile.FullName[0];
                RawDisk disk = new RawDisk(driveLetter);

                NTFSDiskProvider provider = new NTFSDiskProvider(disk);

                NTFS ntfs = new NTFS(provider, 0);

                NtfsDirectory ntfsDir = NTFSHelpers.OpenDir(ntfs, tmpFile.DirectoryName);
                NtfsFile ntfsFile = NTFSHelpers.OpenFile(ntfsDir, tmpFile.Name);

                Assert.IsNotNull(ntfsFile);

                // Read it
                using (Stream actualStream = File.OpenRead(tmpFile.FullName))
                using (Stream ntfsStream = ntfsFile.OpenRead())
                {
                    bool equal = StreamUtils.CompareStreams(actualStream, ntfsStream);

                    Assert.IsTrue(equal);
                }
            }
            finally
            {
                if (tmpFile.Exists)
                    tmpFile.Delete();
            }
        }

        [TestMethod]
        public void SparseFile()
        {
            byte[] randomData = new byte[35 * 4096];
            _random.NextBytes(randomData);

            // Clear the 16 * 4096 -> 32 * 4096 range
            Array.Clear(randomData, 16 * 4096, 16 * 4096);

            FileInfo tmpFile = new FileInfo(Path.GetTempFileName());
            try
            {
                // Create a file
                File.WriteAllBytes(tmpFile.FullName, randomData);

                using (DeviceIOControlWrapper wrapper = Win32.GetFileWrapper(tmpFile.FullName))
                {
                    wrapper.FileSystemSetSparseFile(true);
                    wrapper.FileSystemSetZeroData(16 * 4096, 16 * 4096);

                    FILE_ALLOCATED_RANGE_BUFFER[] rangesData = wrapper.FileSystemQueryAllocatedRanges(0, randomData.Length);

                    // We should have 2 ranges on non-zero data
                    Assert.AreEqual(2, rangesData.Length);
                }

                // Discover it via the NTFS lib
                char driveLetter = tmpFile.FullName[0];
                RawDisk disk = new RawDisk(driveLetter);

                NTFSDiskProvider provider = new NTFSDiskProvider(disk);

                NTFS ntfs = new NTFS(provider, 0);

                NtfsDirectory ntfsDir = NTFSHelpers.OpenDir(ntfs, tmpFile.DirectoryName);
                NtfsFile ntfsFile = NTFSHelpers.OpenFile(ntfsDir, tmpFile.Name);

                Assert.IsNotNull(ntfsFile);
                Assert.IsTrue(tmpFile.Attributes.HasFlag(FileAttributes.SparseFile));
                AttributeData attributeData = ntfsFile.MFTRecord.Attributes.OfType<AttributeData>().Single();
                Assert.IsTrue(attributeData.DataFragments.Length > 1);
                Assert.IsTrue(attributeData.DataFragments.Any(s => s.IsSparseFragment));

                // Read it
                using (Stream actualStream = File.OpenRead(tmpFile.FullName))
                using (Stream ntfsStream = ntfsFile.OpenRead())
                {
                    bool equal = StreamUtils.CompareStreams(actualStream, ntfsStream);

                    Assert.IsTrue(equal);
                }
            }
            finally
            {
                if (tmpFile.Exists)
                    tmpFile.Delete();
            }
        }

        [TestMethod]
        public void CompressedFile()
        {
            FileInfo tmpFile = new FileInfo(Path.GetTempFileName());
            try
            {
                // Create a file
                // Write file data
                using (FileStream fs = File.OpenWrite(tmpFile.FullName))
                {
                    byte[] data = Encoding.ASCII.GetBytes("The white bunny jumps over the brown dog in a carparking lot");

                    for (int i = 0; i < 20000; i++)
                    {
                        fs.Write(data, 0, data.Length);
                    }
                }

                using (DeviceIOControlWrapper wrapper = Win32.GetFileWrapper(tmpFile.FullName))
                {
                    wrapper.FileSystemSetCompression(COMPRESSION_FORMAT.LZNT1);
                }

                // Discover it via the NTFS lib
                char driveLetter = tmpFile.FullName[0];
                RawDisk disk = new RawDisk(driveLetter);

                NTFSDiskProvider provider = new NTFSDiskProvider(disk);

                NTFS ntfs = new NTFS(provider, 0);

                NtfsDirectory ntfsDir = NTFSHelpers.OpenDir(ntfs, tmpFile.DirectoryName);
                NtfsFile ntfsFile = NTFSHelpers.OpenFile(ntfsDir, tmpFile.Name);

                Assert.IsNotNull(ntfsFile);
                Assert.IsTrue(tmpFile.Attributes.HasFlag(FileAttributes.Compressed));
                AttributeData attributeData = ntfsFile.MFTRecord.Attributes.OfType<AttributeData>().Single();
                Assert.IsTrue(attributeData.DataFragments.Any(s => s.IsCompressed));

                // Read it
                using (Stream actualStream = File.OpenRead(tmpFile.FullName))
                using (Stream ntfsStream = ntfsFile.OpenRead())
                {
                    bool equal = StreamUtils.CompareStreams(actualStream, ntfsStream);

                    Assert.IsTrue(equal);
                }
            }
            finally
            {
                if (tmpFile.Exists)
                    tmpFile.Delete();
            }
        }
    }
}
