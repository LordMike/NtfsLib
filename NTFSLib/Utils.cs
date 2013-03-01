using System;
using System.Linq;
using NTFSLib.Objects;

namespace NTFSLib
{
    public static class Utils
    {
        private static long _maxFileTime = DateTime.MaxValue.ToFileTimeUtc();

        public static DateTime FromWinFileTime(byte[] data, int offset)
        {
            long fileTime = BitConverter.ToInt64(data, offset);

            if (fileTime >= _maxFileTime)
                return DateTime.MaxValue;

            return DateTime.FromFileTimeUtc(fileTime);
        }

        public static byte[] ReadFragments(NTFS ntfs, DataFragment[] fragments)
        {
            int totalLength = (int) (fragments.Sum(s => (decimal)s.ClusterCount) * ntfs.BytesPrCluster);

            byte[] data = new byte[totalLength];

            // Get all chunks
            foreach (DataFragment fragment in fragments)
            {
                // Calculate this fragments location on Disk
                ulong offset = fragment.LCN * ntfs.BytesPrCluster;
                int length = (int)(fragment.ClusterCount * ntfs.BytesPrCluster);

                if (!ntfs.Provider.CanReadBytes(offset, length))
                    throw new InvalidOperationException();

                // Get the data
                byte[] fragmentData = ntfs.Provider.ReadBytes(offset, length);

                // Calculate this fragments location in the target array
                int destinationOffset = (int)fragment.StartingVCN * (int)ntfs.BytesPrCluster;

                Array.Copy(fragmentData, 0, data, destinationOffset, Math.Min(fragmentData.Length, data.Length - destinationOffset));
            }

            // Return the data
            return data;
        }
    }
}
