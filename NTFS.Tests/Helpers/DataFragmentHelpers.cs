using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTFSLib.Objects;

namespace NTFS.Tests.Helpers
{
    public static class DataFragmentHelpers
    {
        public static void CheckFragment(DataFragment fragment, int clusterCount, byte compressedClusters, int startingVcn, byte size, int lcn, bool isSparseExtent, bool isCompressedExtent)
        {
            Assert.AreEqual(clusterCount, (int)fragment.ClusterCount);
            Assert.AreEqual(startingVcn, (int)fragment.StartingVCN);
            Assert.AreEqual(size, fragment.Size);
            Assert.AreEqual(lcn, (int)fragment.LCN);
            Assert.AreEqual(compressedClusters, (int)fragment.CompressedClusters);

            Assert.AreEqual(isSparseExtent, fragment.IsSparseFragment);
            Assert.AreEqual(isCompressedExtent, fragment.IsCompressed);
        }
    }
}