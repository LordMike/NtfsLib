using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTFS.Tests.Helpers;
using NTFSLib.Objects;

namespace NTFS.Tests
{
    [TestClass]
    public class DataFragmentActual
    {
        [TestMethod]
        public void ActualFragmentRun1()
        {
            // Mikes Disk E: MFT# 2116 AttributeList Non-Resident
            byte[] data = new byte[] { 0x41, 0x01, 0x34, 0x38, 0x3D, 0x0B, 0x41, 0x02, 0xB2, 0x87, 0x4D, 0x08, 0x31, 0x01, 0x51, 0xD2, 0x83, 0x41, 0x02, 0x26, 0xB5, 0x63, 0xFE, 0x21, 0x02, 0x01, 0xE0, 0x00, 0x00, 0x00, 0x00, 0x00 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 7);

            Assert.AreEqual(5, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 1, 0, 0, 0x41, 188561460, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 2, 0, 1, 0x41, 327860198, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 1, 0, 3, 0x31, 319722039, false, false);
            DataFragmentHelpers.CheckFragment(fragments[3], 2, 0, 4, 0x41, 292702045, false, false);
            DataFragmentHelpers.CheckFragment(fragments[4], 2, 0, 6, 0x21, 292693854, false, false);
        }

        [TestMethod]
        public void ActualFragmentRun2()
        {
            // Mikes Disk E: MFT# 288729 AttributeList Non-Resident
            byte[] data = new byte[] { 0x41, 0x01, 0x30, 0x4C, 0xBE, 0x0D, 0x00, 0xFF };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 0);

            Assert.AreEqual(1, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 1, 0, 0, 0x41, 230575152, false, false);
        }
    }
}
