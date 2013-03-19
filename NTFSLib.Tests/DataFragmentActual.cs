using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTFSLib.Objects;
using NTFSLib.Tests.Helpers;

namespace NTFSLib.Tests
{
    [TestClass]
    public class DataFragmentActual
    {
        [TestMethod]
        public void ActualFragmentRun1()
        {
            // Mikes Disk E: MFT# 2116 AttributeList Non-Resident
            byte[] data = new byte[] { 0x41, 0x01, 0x34, 0x38, 0x3D, 0x0B, 0x41, 0x02, 0xB2, 0x87, 0x4D, 0x08, 0x31, 0x01, 0x51, 0xD2, 0x83, 0x41, 0x02, 0x26, 0xB5, 0x63, 0xFE, 0x21, 0x02, 0x01, 0xE0, 0x00 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 7);

            Assert.AreEqual(5, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 1, 0, 0, 0x41, 188561460, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 2, 0, 1, 0x41, 327860198, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 1, 0, 3, 0x31, 319722039, false, false);
            DataFragmentHelpers.CheckFragment(fragments[3], 2, 0, 4, 0x41, 292702045, false, false);
            DataFragmentHelpers.CheckFragment(fragments[4], 2, 0, 6, 0x21, 292693854, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments);

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void ActualFragmentRun2()
        {
            // Mikes Disk E: MFT# 288729 AttributeList Non-Resident
            byte[] data = new byte[] { 0x41, 0x01, 0x30, 0x4C, 0xBE, 0x0D, 0x00, 0xFF };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 0);

            Assert.AreEqual(1, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 1, 0, 0, 0x41, 230575152, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments);

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void ActualFragmentRun3()
        {
            // Mikes Disk E: MFT# 39 Data Non-Resident
            byte[] data = new byte[] { 0x41, 0x02, 0x41, 0xC3, 0x88, 0x08, 0x31, 0x02, 0xE4, 0x2D, 0x01, 0x31, 0x03, 0x1D, 0x41, 0x01, 0x00, 0x0D, 0x5A, 0x18, 0xA0, 0xF8, 0xFF, 0xFF };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 6);

            Assert.AreEqual(3, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 2, 0, 0, 0x41, 143180609, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 2, 0, 2, 0x31, 143257893, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 3, 0, 4, 0x31, 143340098, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments);

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void ActualFragmentRun4()
        {
            // Mikes Disk E: MFT# 45117 Data Non-Resident
            byte[] data = new byte[] { 0x41, 0x10, 0xC9, 0x2E, 0x8D, 0x08, 0x11, 0x10, 0x13, 0x41, 0x10, 0x44, 0xEC, 0x66, 0x02, 0x41, 0x13, 0x2B, 0x82, 0x25, 0x06, 0x31, 0x5A, 0xE2, 0x32, 0x04, 0x31, 0x03, 0x13, 0xCD, 0xFB, 0x41, 0x14, 0xBB, 0x76, 0x73, 0xF7, 0x21, 0x39, 0xA2, 0x37, 0x21, 0x03, 0x3C, 0xE3, 0x41, 0x14, 0x9C, 0x9B, 0x64, 0x02, 0x21, 0x0C, 0x5E, 0x01, 0x41, 0x15, 0x9E, 0x3F, 0x99, 0xFD, 0x41, 0x0C, 0x23, 0x27, 0x79, 0x0F, 0x41, 0x15, 0x57, 0x95, 0x88, 0xF0, 0x21, 0x0A, 0x06, 0x4C, 0x31, 0x17, 0xB6, 0xBC, 0xFD, 0x31, 0x09, 0xAF, 0xEA, 0x08, 0x41, 0x17, 0x5C, 0x54, 0x41, 0x08, 0x31, 0x2C, 0x6C, 0x08, 0x47, 0x41, 0x0E, 0x74, 0xA3, 0x99, 0xF8, 0x41, 0x18, 0xBE, 0xA9, 0xD4, 0xFE, 0x41, 0x07, 0xF4, 0x37, 0x8F, 0x08, 0x41, 0x18, 0x7D, 0x1B, 0x71, 0xF7, 0x31, 0x08, 0x1F, 0x8A, 0x01, 0x21, 0x18, 0x85, 0x61, 0x31, 0x08, 0x93, 0xCD, 0x00, 0x41, 0x18, 0xF4, 0x9D, 0x47, 0x08, 0x42, 0x8A, 0x00, 0xAA, 0x0A, 0x1F, 0xFA, 0x31, 0x0E, 0x11, 0xB1, 0xFD, 0x41, 0x19, 0x68, 0x48, 0x9B, 0xFD, 0x31, 0x08, 0xF9, 0xBE, 0xE2, 0x31, 0x1B, 0xEA, 0xBB, 0x1A, 0x21, 0x05, 0x72, 0xF2, 0x21, 0x18, 0x45, 0x37, 0x31, 0x08, 0x0F, 0x1B, 0xE5, 0x11, 0x1C, 0xD8, 0x31, 0x04, 0xF8, 0x3C, 0x15, 0x31, 0x1C, 0x89, 0xB2, 0x06, 0x31, 0x04, 0xB4, 0x6B, 0x01, 0x31, 0x1C, 0xD3, 0x43, 0x07, 0x31, 0x04, 0x88, 0x88, 0x02, 0x41, 0x1C, 0xAE, 0x14, 0x3E, 0x08, 0x41, 0x04, 0x56, 0x8B, 0xC2, 0xF7, 0x41, 0x1C, 0x00, 0x02, 0x82, 0x08, 0x41, 0x04, 0x8C, 0x9F, 0xD9, 0xF9, 0x41, 0x1E, 0x56, 0x61, 0x26, 0x06, 0x31, 0x03, 0xC5, 0x71, 0xBB, 0x41, 0x20, 0xEE, 0x35, 0xB5, 0xF7, 0x31, 0x20, 0x75, 0x3A, 0x01, 0x31, 0x0E, 0x16, 0xAB, 0x08, 0x31, 0x10, 0x58, 0x50, 0xF7, 0x31, 0x10, 0x6B, 0x54, 0xE4, 0x11, 0x10, 0xEE, 0x11, 0x10, 0xEE, 0x11, 0x10, 0xEE, 0x11, 0x10, 0xEE, 0x31, 0x10, 0x49, 0x04, 0xFC, 0x31, 0x11, 0x11, 0xFC, 0x03, 0x11, 0x10, 0x96, 0x31, 0x0F, 0xB5, 0xE8, 0xF5, 0x31, 0x11, 0x6D, 0xE1, 0x0C, 0x32, 0x2F, 0x1A, 0x08, 0x08, 0x09, 0x00, 0x00, 0x00, 0xA0, 0x00, 0x05, 0x04 };
            DataFragment[] fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 7967);

            Assert.AreEqual(62, fragments.Length);

            DataFragmentHelpers.CheckFragment(fragments[0], 16, 0, 0, 0x41, 143470281, false, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 16, 0, 16, 0x11, 143470300, false, false);
            DataFragmentHelpers.CheckFragment(fragments[2], 16, 0, 32, 0x41, 183769888, false, false);
            DataFragmentHelpers.CheckFragment(fragments[3], 19, 0, 48, 0x41, 286891339, false, false);
            DataFragmentHelpers.CheckFragment(fragments[4], 90, 0, 67, 0x31, 287166509, false, false);
            DataFragmentHelpers.CheckFragment(fragments[5], 3, 0, 157, 0x31, 286891328, false, false);
            DataFragmentHelpers.CheckFragment(fragments[6], 20, 0, 160, 0x41, 143463419, false, false);
            DataFragmentHelpers.CheckFragment(fragments[7], 57, 0, 180, 0x21, 143477661, false, false);
            DataFragmentHelpers.CheckFragment(fragments[8], 3, 0, 237, 0x21, 143470297, false, false);
            DataFragmentHelpers.CheckFragment(fragments[9], 20, 0, 240, 0x41, 183618165, false, false);
            DataFragmentHelpers.CheckFragment(fragments[10], 12, 0, 260, 0x21, 183618515, false, false);
            DataFragmentHelpers.CheckFragment(fragments[11], 21, 0, 272, 0x41, 143330161, false, false);
            DataFragmentHelpers.CheckFragment(fragments[12], 12, 0, 293, 0x41, 402928276, false, false);
            DataFragmentHelpers.CheckFragment(fragments[13], 21, 0, 305, 0x41, 143443947, false, false);
            DataFragmentHelpers.CheckFragment(fragments[14], 10, 0, 326, 0x21, 143463409, false, false);
            DataFragmentHelpers.CheckFragment(fragments[15], 23, 0, 336, 0x31, 143315111, false, false);
            DataFragmentHelpers.CheckFragment(fragments[16], 9, 0, 359, 0x31, 143899478, false, false);
            DataFragmentHelpers.CheckFragment(fragments[17], 23, 0, 368, 0x41, 282398642, false, false);
            DataFragmentHelpers.CheckFragment(fragments[18], 44, 0, 391, 0x31, 287053854, false, false);
            DataFragmentHelpers.CheckFragment(fragments[19], 14, 0, 435, 0x41, 162904978, false, false);
            DataFragmentHelpers.CheckFragment(fragments[20], 24, 0, 449, 0x41, 143287632, false, false);
            DataFragmentHelpers.CheckFragment(fragments[21], 7, 0, 473, 0x41, 286891332, false, false);
            DataFragmentHelpers.CheckFragment(fragments[22], 24, 0, 480, 0x41, 143308993, false, false);
            DataFragmentHelpers.CheckFragment(fragments[23], 8, 0, 504, 0x31, 143409888, false, false);
            DataFragmentHelpers.CheckFragment(fragments[24], 24, 0, 512, 0x21, 143434853, false, false);
            DataFragmentHelpers.CheckFragment(fragments[25], 8, 0, 536, 0x31, 143487480, false, false);
            DataFragmentHelpers.CheckFragment(fragments[26], 24, 0, 544, 0x41, 282398700, false, false);
            DataFragmentHelpers.CheckFragment(fragments[27], 138, 0, 568, 0x42, 183769750, false, false);
            DataFragmentHelpers.CheckFragment(fragments[28], 14, 0, 706, 0x31, 183618471, false, false);
            DataFragmentHelpers.CheckFragment(fragments[29], 25, 0, 720, 0x41, 143463439, false, false);
            DataFragmentHelpers.CheckFragment(fragments[30], 8, 0, 745, 0x31, 141546248, false, false);
            DataFragmentHelpers.CheckFragment(fragments[31], 27, 0, 753, 0x31, 143298290, false, false);
            DataFragmentHelpers.CheckFragment(fragments[32], 5, 0, 780, 0x21, 143294820, false, false);
            DataFragmentHelpers.CheckFragment(fragments[33], 24, 0, 785, 0x21, 143308969, false, false);
            DataFragmentHelpers.CheckFragment(fragments[34], 8, 0, 809, 0x31, 141546424, false, false);
            DataFragmentHelpers.CheckFragment(fragments[35], 28, 0, 817, 0x11, 141546384, false, false);
            DataFragmentHelpers.CheckFragment(fragments[36], 4, 0, 845, 0x31, 142938248, false, false);
            DataFragmentHelpers.CheckFragment(fragments[37], 28, 0, 849, 0x31, 143377169, false, false);
            DataFragmentHelpers.CheckFragment(fragments[38], 4, 0, 877, 0x31, 143470277, false, false);
            DataFragmentHelpers.CheckFragment(fragments[39], 28, 0, 881, 0x31, 143946392, false, false);
            DataFragmentHelpers.CheckFragment(fragments[40], 4, 0, 909, 0x31, 144112416, false, false);
            DataFragmentHelpers.CheckFragment(fragments[41], 28, 0, 913, 0x41, 282398670, false, false);
            DataFragmentHelpers.CheckFragment(fragments[42], 4, 0, 941, 0x41, 144153380, false, false);
            DataFragmentHelpers.CheckFragment(fragments[43], 28, 0, 945, 0x41, 286891300, false, false);
            DataFragmentHelpers.CheckFragment(fragments[44], 4, 0, 973, 0x41, 183712944, false, false);
            DataFragmentHelpers.CheckFragment(fragments[45], 30, 0, 977, 0x41, 286891526, false, false);
            DataFragmentHelpers.CheckFragment(fragments[46], 3, 0, 1007, 0x31, 282398667, false, false);
            DataFragmentHelpers.CheckFragment(fragments[47], 32, 0, 1010, 0x41, 143279545, false, false);
            DataFragmentHelpers.CheckFragment(fragments[48], 32, 0, 1042, 0x31, 143360046, false, false);
            DataFragmentHelpers.CheckFragment(fragments[49], 14, 0, 1074, 0x31, 143928132, false, false);
            DataFragmentHelpers.CheckFragment(fragments[50], 16, 0, 1088, 0x31, 143358876, false, false);
            DataFragmentHelpers.CheckFragment(fragments[51], 16, 0, 1104, 0x31, 141545479, false, false);
            DataFragmentHelpers.CheckFragment(fragments[52], 16, 0, 1120, 0x11, 141545461, false, false);
            DataFragmentHelpers.CheckFragment(fragments[53], 16, 0, 1136, 0x11, 141545443, false, false);
            DataFragmentHelpers.CheckFragment(fragments[54], 16, 0, 1152, 0x11, 141545425, false, false);
            DataFragmentHelpers.CheckFragment(fragments[55], 16, 0, 1168, 0x11, 141545407, false, false);
            DataFragmentHelpers.CheckFragment(fragments[56], 16, 0, 1184, 0x31, 141284360, false, false);
            DataFragmentHelpers.CheckFragment(fragments[57], 17, 0, 1200, 0x31, 141545497, false, false);
            DataFragmentHelpers.CheckFragment(fragments[58], 16, 0, 1217, 0x11, 141545391, false, false);
            DataFragmentHelpers.CheckFragment(fragments[59], 15, 0, 1233, 0x31, 140884068, false, false);
            DataFragmentHelpers.CheckFragment(fragments[60], 17, 0, 1248, 0x31, 141728209, false, false);
            DataFragmentHelpers.CheckFragment(fragments[61], 6703, 0, 1265, 0x32, 142320089, false, false);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments);

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }

        [TestMethod]
        public void ActualFragmentRun5()
        {
            // Mikes Disk C: MFT# -- Data Non-Resident (Sparse & Compressed)
            byte[] data = new byte[] { 0x01, 0x60, 0x41, 0x02, 0x6C, 0xB3, 0xAA, 0x00, 0x01, 0x0E, 0x11, 0x01, 0x10, 0x01, 0x0F, 0x00 };
            List<DataFragment> fragments = DataFragment.ParseFragments(data, data.Length, 0, 0, 127).ToList();
            DataFragment.CompactCompressedFragments(fragments);

            Assert.AreEqual(3, fragments.Count);

            DataFragmentHelpers.CheckFragment(fragments[0], 96, 0, 0, 0x01, 0, true, false);
            DataFragmentHelpers.CheckFragment(fragments[1], 2, 14, 96, 0x41, 11187052, false, true);
            DataFragmentHelpers.CheckFragment(fragments[2], 1, 15, 112, 0x11, 11187068, false, true);

            // Save to bytes
            byte[] newData = DataFragmentHelpers.SaveFragments(fragments.ToArray());

            Assert.IsTrue(newData.SequanceEqualIn(data));
        }
    }
}

