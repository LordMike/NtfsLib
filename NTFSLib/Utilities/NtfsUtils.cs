using System;
using System.Diagnostics;
using System.Linq;
using NTFSLib.IO;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Specials;

namespace NTFSLib.Utilities
{
    public static class NtfsUtils
    {
        private static readonly long MaxFileTime = DateTime.MaxValue.ToFileTimeUtc();

        public static DateTime FromWinFileTime(byte[] data, int offset)
        {
            long fileTime = BitConverter.ToInt64(data, offset);

            if (fileTime >= MaxFileTime)
                return DateTime.MaxValue;

            return DateTime.FromFileTimeUtc(fileTime);
        }

        public static byte[] ReadFragments(NTFS ntfs, DataFragment[] fragments)
        {
            int totalLength = (int)(fragments.Sum(s => (decimal)s.Clusters) * ntfs.BytesPrCluster);
            byte[] data = new byte[totalLength];

            using (NtfsDiskStream stream = new NtfsDiskStream(ntfs, ntfs.Provider.CreateDiskStream(), fragments, 0, totalLength))
            {
                stream.Read(data, 0, data.Length);
            }

            // Return the data
            return data;
        }

        public static void ApplyUSNPatch(byte[] data, int offset, int sectors, ushort bytesPrSector, byte[] usnNumber, byte[] usnData)
        {
            Debug.Assert(data.Length >= offset + sectors * bytesPrSector);
            Debug.Assert(usnNumber.Length == 2);
            Debug.Assert(sectors * 2 <= usnData.Length);

            for (int i = 0; i < sectors; i++)
            {
                // Get pointer to the last two bytes
                int blockOffset = offset + i * bytesPrSector + 510;

                // Check that they match the USN Number
                Debug.Assert(data[blockOffset] == usnNumber[0]);
                Debug.Assert(data[blockOffset + 1] == usnNumber[1]);

                // Patch in new data
                data[blockOffset] = usnData[i * 2];
                data[blockOffset + 1] = usnData[i * 2 + 1];
            }
        }

        public static AttributeFileName GetPreferredDisplayName(FileRecord record)
        {
            return record.Attributes.OfType<AttributeFileName>().OrderByDescending(s => s.FilenameNamespace, new FileNamespaceComparer()).FirstOrDefault();
        }
    }
}
