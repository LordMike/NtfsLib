using System;
using System.Diagnostics;
using System.IO;
using NtfsExtract.NTFS.IO;
using NtfsExtract.NTFS.Objects;
using RawDiskLib;

namespace NtfsExtract.NTFS.Utilities
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

        public static void ToWinFileTime(byte[] data, int offset, DateTime dateTime)
        {
            if (dateTime == DateTime.MaxValue)
            {
                LittleEndianConverter.GetBytes(data, offset, long.MaxValue);
            }
            else
            {
                long fileTime = dateTime.ToFileTimeUtc();

                LittleEndianConverter.GetBytes(data, offset, fileTime);
            }
        }

        public static byte[] ReadFragments(RawDisk disk, DataFragment[] fragments)
        {
            int totalLength = 0;
            foreach (DataFragment fragment in fragments)
                totalLength += (int)(fragment.Clusters * disk.ClusterSize);

            byte[] data = new byte[totalLength];

            using (Stream diskStream = disk.CreateDiskStream())
            using (NtfsDiskStream stream = new NtfsDiskStream(diskStream, false, fragments, (uint)disk.ClusterSize, 0, totalLength))
            {
                stream.Read(data, 0, data.Length);
            }

            // Return the data
            return data;
        }

        public static void ApplyUSNPatch(byte[] data, int offset, uint sectors, ushort bytesPrSector, byte[] usnNumber, byte[] usnData)
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

        //public static AttributeFileName GetPreferredDisplayName(FileRecord record)
        //{
        //    return GetPreferredDisplayName(record.Attributes);
        //}

        //public static AttributeFileName GetPreferredDisplayName(IEnumerable<Attribute> attributes)
        //{
        //    return attributes.OfType<AttributeFileName>().OrderByDescending(s => s.FilenameNamespace, new FileNamespaceComparer()).FirstOrDefault();
        //}
    }
}
