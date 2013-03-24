using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NTFSLib.NTFS;
using NTFSLib.Objects.Enums;
using NTFSLib.Utilities;

namespace NTFSLib.Objects.Specials
{
    public class IndexAllocationChunk
    {
        public string Signature { get; set; }
        public ushort OffsetToUSN { get; set; }
        public ushort USNSizeWords { get; set; }
        public ulong LogFileUSN { get; set; }
        public ulong VCNInIndexAllocation { get; set; }
        public uint OffsetToFirstIndex { get; set; }
        public uint SizeOfIndexTotal { get; set; }
        public uint SizeOfIndexAllocated { get; set; }
        public byte HasChildren { get; set; }
        public byte[] USNNumber { get; set; }
        public byte[] USNData { get; set; }

        public IndexEntry[] Entries { get; set; }

        public static bool IsIndexAllocationChunk(byte[] data, int offset)
        {
            Debug.Assert(data.Length - offset >= 4);
            string signature = Encoding.ASCII.GetString(data, offset + 0, 4);

            return signature == "INDX";
        }

        public static IndexAllocationChunk ParseBody(INTFSInfo ntfsInfo, byte[] data, int offset)
        {
            Debug.Assert(data.Length >= 36);

            IndexAllocationChunk res = new IndexAllocationChunk();

            // Parse
            res.Signature = Encoding.ASCII.GetString(data, offset + 0, 4);

            Debug.Assert(res.Signature == "INDX");

            res.OffsetToUSN = BitConverter.ToUInt16(data, offset + 4);
            res.USNSizeWords = BitConverter.ToUInt16(data, offset + 6);
            res.LogFileUSN = BitConverter.ToUInt64(data, offset + 8);
            res.VCNInIndexAllocation = BitConverter.ToUInt64(data, offset + 16);
            res.OffsetToFirstIndex = BitConverter.ToUInt32(data, offset + 24);
            res.SizeOfIndexTotal = BitConverter.ToUInt32(data, offset + 28);
            res.SizeOfIndexAllocated = BitConverter.ToUInt32(data, offset + 32);
            res.HasChildren = data[36];

            Debug.Assert(data.Length >= offset + res.OffsetToUSN + 2 + res.USNSizeWords * 2);

            res.USNNumber = new byte[2];
            Array.Copy(data, offset + res.OffsetToUSN, res.USNNumber, 0, 2);

            res.USNData = new byte[res.USNSizeWords * 2 - 2];
            Array.Copy(data, offset + res.OffsetToUSN + 2, res.USNData, 0, res.USNData.Length);

            // Patch USN Data
            NtfsUtils.ApplyUSNPatch(data, offset, (res.SizeOfIndexAllocated + 24) / ntfsInfo.BytesPrSector, (ushort)ntfsInfo.BytesPrSector, res.USNNumber, res.USNData);

            Debug.Assert(offset + res.SizeOfIndexTotal <= data.Length);

            // Parse entries
            List<IndexEntry> entries = new List<IndexEntry>();

            int pointer = offset + (int)(res.OffsetToFirstIndex + 24);       // Offset is relative to 0x18
            while (pointer <= offset + res.SizeOfIndexTotal + 24)
            {
                IndexEntry entry = IndexEntry.ParseData(data, offset + (int)res.SizeOfIndexTotal - pointer + 24, pointer);

                if (entry.Flags.HasFlag(MFTIndexEntryFlags.LastEntry))
                    break;

                entries.Add(entry);

                pointer += entry.Size;
            }

            res.Entries = entries.ToArray();

            return res;
        }
    }
}