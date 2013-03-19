using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTFSLib.Utilities;
using System.Linq;

namespace NTFSLib.Tests
{
    [TestClass]
    public class BitConvertTests
    {
        [TestMethod]
        public void TestShort()
        {
            short[] specials = new short[] { short.MinValue, -1, 0, 1, short.MaxValue };

            foreach (short special in specials)
            {
                byte[] buffA = new byte[2];
                LittleEndianConverter.GetBytes(buffA, 0, special);

                byte[] buffB = BitConverter.GetBytes(special);

                Assert.IsTrue(buffA.SequenceEqual(buffB));
            }
        }

        [TestMethod]
        public void TestUShort()
        {
            ushort[] specials = new ushort[] { ushort.MinValue, 1, ushort.MaxValue };

            foreach (ushort special in specials)
            {
                byte[] buffA = new byte[2];
                LittleEndianConverter.GetBytes(buffA, 0, special);

                byte[] buffB = BitConverter.GetBytes(special);

                Assert.IsTrue(buffA.SequenceEqual(buffB));
            }
        }

        [TestMethod]
        public void TestInt()
        {
            int[] specials = new[] { int.MinValue, -1, 0, 1, int.MaxValue };

            foreach (int special in specials)
            {
                byte[] buffA = new byte[4];
                LittleEndianConverter.GetBytes(buffA, 0, special);

                byte[] buffB = BitConverter.GetBytes(special);

                Assert.IsTrue(buffA.SequenceEqual(buffB));
            }
        }

        [TestMethod]
        public void TestUInt()
        {
            uint[] specials = new uint[] { uint.MinValue, 1, uint.MaxValue };

            foreach (uint special in specials)
            {
                byte[] buffA = new byte[4];
                LittleEndianConverter.GetBytes(buffA, 0, special);

                byte[] buffB = BitConverter.GetBytes(special);

                Assert.IsTrue(buffA.SequenceEqual(buffB));
            }
        }

        [TestMethod]
        public void TestLong()
        {
            long[] specials = new[] { long.MinValue, -1, 0, 1, long.MaxValue };

            foreach (long special in specials)
            {
                byte[] buffA = new byte[8];
                LittleEndianConverter.GetBytes(buffA, 0, special);

                byte[] buffB = BitConverter.GetBytes(special);

                Assert.IsTrue(buffA.SequenceEqual(buffB));
            }
        }

        [TestMethod]
        public void TestULong()
        {
            ulong[] specials = new ulong[] { ulong.MinValue, 1, ulong.MaxValue };

            foreach (ulong special in specials)
            {
                byte[] buffA = new byte[8];
                LittleEndianConverter.GetBytes(buffA, 0, special);

                byte[] buffB = BitConverter.GetBytes(special);

                Assert.IsTrue(buffA.SequenceEqual(buffB));
            }
        }
    }
}
