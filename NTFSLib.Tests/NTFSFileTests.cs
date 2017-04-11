using System;
using System.IO;
using System.Linq;
using System.Text;
using DeviceIOControlLib;
using DeviceIOControlLib.Objects.FileSystem;
using DeviceIOControlLib.Wrapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTFSLib.Helpers;
using NTFSLib.IO;
using NTFSLib.NTFS;
using NTFSLib.Objects.Attributes;
using NTFSLib.Tests.Helpers;
using RawDiskLib;
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
            
            using (TempFile tmpFile = new TempFile())
            {
                // Create a file
                File.WriteAllBytes(tmpFile.File.FullName, randomData);

                // Discover it via the NTFS lib
                char driveLetter = tmpFile.File.FullName[0];
                RawDisk disk = new RawDisk(driveLetter);

                NTFSDiskProvider provider = new NTFSDiskProvider(disk);

                NTFSWrapper ntfsWrapper = new NTFSWrapper(provider, 0);

                NtfsDirectory ntfsDir = NTFSHelpers.OpenDir(ntfsWrapper, tmpFile.File.DirectoryName);
                NtfsFile ntfsFile = NTFSHelpers.OpenFile(ntfsDir, tmpFile.File.Name);

                Assert.IsNotNull(ntfsFile);

                // Read it
                using (Stream actualStream = File.OpenRead(tmpFile.File.FullName))
                using (Stream ntfsStream = ntfsFile.OpenRead())
                {
                    bool equal = StreamUtils.CompareStreams(actualStream, ntfsStream);

                    Assert.IsTrue(equal);
                }
            }
        }

        [TestMethod]
        public void SparseFile()
        {
            byte[] randomData = new byte[35 * 4096];
            _random.NextBytes(randomData);

            // Clear the 16 * 4096 -> 32 * 4096 range
            Array.Clear(randomData, 16 * 4096, 16 * 4096);
            
            using (TempFile tmpFile = new TempFile())
            {
                // Create a file
                File.WriteAllBytes(tmpFile.File.FullName, randomData);

                using (FilesystemDeviceWrapper wrapper = Win32.GetFileWrapper(tmpFile.File.FullName))
                {
                    wrapper.FileSystemSetSparseFile(true);
                    wrapper.FileSystemSetZeroData(16 * 4096, 16 * 4096);

                    FILE_ALLOCATED_RANGE_BUFFER[] rangesData = wrapper.FileSystemQueryAllocatedRanges(0, randomData.Length);

                    // We should have 2 ranges on non-zero data
                    Assert.AreEqual(2, rangesData.Length);
                }

                // Discover it via the NTFS lib
                char driveLetter = tmpFile.File.FullName[0];
                RawDisk disk = new RawDisk(driveLetter);

                NTFSDiskProvider provider = new NTFSDiskProvider(disk);

                NTFSWrapper ntfsWrapper = new NTFSWrapper(provider, 0);

                NtfsDirectory ntfsDir = NTFSHelpers.OpenDir(ntfsWrapper, tmpFile.File.DirectoryName);
                NtfsFile ntfsFile = NTFSHelpers.OpenFile(ntfsDir, tmpFile.File.Name);

                Assert.IsNotNull(ntfsFile);
                Assert.IsTrue(tmpFile.File.Attributes.HasFlag(FileAttributes.SparseFile));
                AttributeData attributeData = ntfsFile.MFTRecord.Attributes.OfType<AttributeData>().Single();
                Assert.IsTrue(attributeData.DataFragments.Length > 1);
                Assert.IsTrue(attributeData.DataFragments.Any(s => s.IsSparseFragment));

                // Read it
                using (Stream actualStream = File.OpenRead(tmpFile.File.FullName))
                using (Stream ntfsStream = ntfsFile.OpenRead())
                {
                    bool equal = StreamUtils.CompareStreams(actualStream, ntfsStream);

                    Assert.IsTrue(equal);
                }
            }
        }

        [TestMethod]
        public void CompressedFile()
        {
            using (TempFile tmpFile = new TempFile())
            {
                // Create a file
                // Write file data
                using (FileStream fs = File.OpenWrite(tmpFile.File.FullName))
                {
                    byte[] data = Encoding.ASCII.GetBytes("The white bunny jumps over the brown dog in a carparking lot");

                    for (int i = 0; i < 20000; i++)
                    {
                        fs.Write(data, 0, data.Length);
                    }
                }

                using (FilesystemDeviceWrapper wrapper = Win32.GetFileWrapper(tmpFile.File.FullName))
                {
                    wrapper.FileSystemSetCompression(COMPRESSION_FORMAT.LZNT1);
                }

                // Discover it via the NTFS lib
                char driveLetter = tmpFile.File.FullName[0];
                RawDisk disk = new RawDisk(driveLetter);

                NTFSDiskProvider provider = new NTFSDiskProvider(disk);

                NTFSWrapper ntfsWrapper = new NTFSWrapper(provider, 0);

                NtfsDirectory ntfsDir = NTFSHelpers.OpenDir(ntfsWrapper, tmpFile.File.DirectoryName);
                NtfsFile ntfsFile = NTFSHelpers.OpenFile(ntfsDir, tmpFile.File.Name);

                Assert.IsNotNull(ntfsFile);
                Assert.IsTrue(tmpFile.File.Attributes.HasFlag(FileAttributes.Compressed));
                AttributeData attributeData = ntfsFile.MFTRecord.Attributes.OfType<AttributeData>().Single();
                Assert.IsTrue(attributeData.DataFragments.Any(s => s.IsCompressed));

                // Read it
                using (Stream actualStream = File.OpenRead(tmpFile.File.FullName))
                using (Stream ntfsStream = ntfsFile.OpenRead())
                {
                    bool equal = StreamUtils.CompareStreams(actualStream, ntfsStream);

                    Assert.IsTrue(equal);
                }
            }
        }
    }
}
