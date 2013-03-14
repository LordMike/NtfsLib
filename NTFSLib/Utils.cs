using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Specials;

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
            long vcn = fragments[0].StartingVCN;
            for (int i = 0; i < fragments.Length; i++)
            {
                Debug.Assert(fragments[i].StartingVCN == vcn);
                vcn += fragments[i].Clusters;// +_fragments[i].CompressedClusters;     // Todo: Handle compressed clusters
            }

            int totalLength = (int)(fragments.Sum(s => (decimal)s.Clusters) * ntfs.BytesPrCluster);

            byte[] data = new byte[totalLength];

            // Get all chunks
            foreach (DataFragment fragment in fragments)
            {
                // Calculate this fragments location on Disk
                long offset = fragment.LCN * ntfs.BytesPrCluster;
                int length = (int)fragment.Clusters * (int)ntfs.BytesPrCluster;

                if (!ntfs.Provider.CanReadBytes((ulong)offset, length))
                    throw new InvalidOperationException();

                // Get the data
                byte[] fragmentData = new byte[length];
                ntfs.Provider.ReadBytes(fragmentData, 0, (ulong)offset, length);

                // Calculate this fragments location in the target array - take the startingVCN of the entire fragmentset into consideration
                int destinationOffset = (int)(fragment.StartingVCN - fragments[0].StartingVCN) * (int)ntfs.BytesPrCluster;

                Array.Copy(fragmentData, 0, data, destinationOffset, Math.Min(fragmentData.Length, data.Length - destinationOffset));
            }

            // Return the data
            return data;
        }

        public static void ApplyUSNPatch(byte[] data, int offset, int sectors, ushort bytesPrSector, byte[] USNNumber, byte[] USNData)
        {
            Debug.Assert(data.Length >= offset + sectors * bytesPrSector);
            Debug.Assert(USNNumber.Length == 2);
            Debug.Assert(sectors * 2 <= USNData.Length);

            for (int i = 0; i < sectors; i++)
            {
                // Get pointer to the last two bytes
                int blockOffset = offset + i * bytesPrSector + 510;

                // Check that they match the USN Number
                Debug.Assert(data[blockOffset] == USNNumber[0]);
                Debug.Assert(data[blockOffset + 1] == USNNumber[1]);

                // Patch in new data
                data[blockOffset] = USNData[i * 2];
                data[blockOffset + 1] = USNData[i * 2 + 1];
            }
        }

        public static AttributeFileName GetPreferredDisplayName(FileRecord record)
        {
            return record.Attributes.OfType<AttributeFileName>().OrderByDescending(s => s.FilenameNamespace, new FileNamespaceComparer()).FirstOrDefault();
        }
    }
}
