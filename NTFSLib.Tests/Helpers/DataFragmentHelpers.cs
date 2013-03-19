using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTFSLib.Objects;
using System.Linq;

namespace NTFSLib.Tests.Helpers
{
    public static class DataFragmentHelpers
    {
        public static void CheckFragment(DataFragment fragment, int clusterCount, byte compressedClusters, int startingVcn, byte size, int lcn, bool isSparseExtent, bool isCompressedExtent)
        {
            Assert.AreEqual(clusterCount, (int)fragment.Clusters);
            Assert.AreEqual(startingVcn, (int)fragment.StartingVCN);
            Assert.AreEqual(size, fragment.FragmentSizeBytes);
            Assert.AreEqual(lcn, (int)fragment.LCN);
            Assert.AreEqual(compressedClusters, (int)fragment.CompressedClusters);

            Assert.AreEqual(isSparseExtent, fragment.IsSparseFragment);
            Assert.AreEqual(isCompressedExtent, fragment.IsCompressed);
        }

        public static byte[] SaveFragments(DataFragment[] fragments)
        {
            // Sum up the expected # of bytes needed. As compressed fragments have been compacted they have been removed - so add two bytes for each compressed fragment to compensate.
            int expectedLength = fragments.Sum(s => s.ThisObjectLength + (s.IsCompressed ? 2 : 0));

            int saveLength = DataFragment.GetSaveLength(fragments);

            Assert.AreEqual(expectedLength, saveLength);

            byte[] data = new byte[saveLength + 1];
            DataFragment.Save(data, 0, fragments);

            return data;
        }
    }
}