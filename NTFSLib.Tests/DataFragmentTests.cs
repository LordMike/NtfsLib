using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTFSLib.Objects;
using NTFSLib.Tests.Helpers;

namespace NTFSLib.Tests
{
    /// <summary>
    /// Implements tests from http://inform.pucp.edu.pe/~inf232/Ntfs/ntfs_doc_v0.5/concepts/data_runs.html
    /// </summary>
    [TestClass]
    public class DataFragmentTests
    {
        [TestMethod]
        public void FragmentsRun1()
        {
            byte[] data = new byte[] { 0x21, 0x18, 0x34, 0x56, 0x00, 0x00 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 23);

            Assert.AreEqual(1, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 24, 0, 0, 0x21, 22068, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void FragmentsRun2()
        {
            byte[] data = new byte[] { 0x31, 0x38, 0x73, 0x25, 0x34, 0x32, 0x14, 0x01, 0xE5, 0x11, 0x02, 0x31, 0x42, 0xAA, 0x00, 0x03, 0x00 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 397);

            Assert.AreEqual(3, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 56, 0, 0, 0x31, 3417459, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 276, 0, 56, 0x32, 3553112, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 66, 0, 332, 0x31, 3749890, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void FragmentsRun3()
        {
            byte[] data = new byte[] { 0x11, 0x30, 0x60, 0x21, 0x10, 0x00, 0x01, 0x11, 0x20, 0xE0, 0x00 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 95);

            Assert.AreEqual(3, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 48, 0, 0, 0x11, 96, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 16, 0, 48, 0x21, 352, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 32, 0, 64, 0x11, 320, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void FragmentsRun4()
        {
            byte[] data = new byte[] { 0x11, 0x30, 0x20, 0x01, 0x60, 0x11, 0x10, 0x30, 0x00 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 159);

            Assert.AreEqual(3, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 48, 0, 0, 0x11, 32, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 96, 0, 48, 0x01, 0, true, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 16, 0, 144, 0x11, 80, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void FragmentsRun5()
        {
            byte[] data = new byte[] { 0x11, 0x08, 0x40, 0x01, 0x08, 0x11, 0x10, 0x08, 0x11, 0x0C, 0x10, 0x01, 0x04, 0x00 };
            List<DataFragment> fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 47).ToList();

            // These fragments have compression
            DataFragment.CompactCompressedFragments(fragments);

            Assert.AreEqual(3, fragments.Count);

            DataFragmentHelpers.CheckFragment(fragments[0], 8, 8, 0, 0x11, 64, false, true);
            DataFragmentHelpers.CheckFragment(fragments[1], 16, 0, 16, 0x11, 72, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 12, 4, 32, 0x11, 88, false, true);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void FragmentsRun6()
        {
            byte[] data = new byte[] { 0x21, 0x14, 0x00, 0x01, 0x11, 0x10, 0x18, 0x11, 0x05, 0x15, 0x01, 0x27, 0x11, 0x20, 0x05, 0x00 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 111);

            Assert.AreEqual(5, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 20, 0, 0, 0x21, 256, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 16, 0, 20, 0x11, 280, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 5, 0, 36, 0x11, 301, false, false);
            DataFragmentHelpers.CheckFragment(fragments[3], 39, 0, 41, 0x01, 0, true, false);
            DataFragmentHelpers.CheckFragment(fragments[4], 32, 0, 80, 0x11, 306, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void FragmentsRun7()
        {
            byte[] data = new byte[] { 0x21, 0x20, 0xED, 0x05, 0x22, 0x48, 0x07, 0x48, 0x22, 0x21, 0x28, 0xC8, 0xDB, 0x00 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 1935);

            Assert.AreEqual(3, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 32, 0, 0, 0x21, 1517, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 1864, 0, 32, 0x22, 10293, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 40, 0, 1896, 0x21, 1021, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void FragmentsRun8()
        {
            byte[] data = new byte[] { 0x11, 0x30, 0x60, 0x21, 0x10, 0x00, 0x01, 0x11, 0x20, 0xE0, 0x00 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 95);

            Assert.AreEqual(3, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 48, 0, 0, 0x11, 96, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 16, 0, 48, 0x21, 352, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 32, 0, 64, 0x11, 320, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void FragmentsRun9()
        {
            byte[] data = new byte[] { 0x21, 0x02, 0xEF, 0x07, 0x01, 0x0E, 0x00 };
            List<DataFragment> fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 15).ToList();

            // These fragments have compression
            DataFragment.CompactCompressedFragments(fragments);

            Assert.AreEqual(1, fragments.Count);

            DataFragmentHelpers.CheckFragment(fragments[0], 2, 14, 0, 0x21, 2031, false, true);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }
    }
}
