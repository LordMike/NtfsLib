using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeIndexAllocation : Attribute
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

        public override AttributeResidentAllow AllowedResidentStates
        {
            get
            {
                return AttributeResidentAllow.NonResident;
            }
        }

        internal override void ParseAttributeNonResidentBody(NTFS ntfs)
        {
            base.ParseAttributeNonResidentBody(ntfs);

            // Get all chunks
            byte[] data = Utils.ReadFragments(ntfs, NonResidentHeader.Fragments);

            Debug.Assert(data.Length >= 36);

            // Parse
            Signature = Encoding.ASCII.GetString(data, 0, 4);

            Debug.Assert(Signature == "INDX");

            OffsetToUSN = BitConverter.ToUInt16(data, 4);
            USNSizeWords = BitConverter.ToUInt16(data, 6);
            LogFileUSN = BitConverter.ToUInt64(data, 8);
            VCNInIndexAllocation = BitConverter.ToUInt64(data, 16);
            OffsetToFirstIndex = BitConverter.ToUInt32(data, 24);
            SizeOfIndexTotal = BitConverter.ToUInt32(data, 28);
            SizeOfIndexAllocated = BitConverter.ToUInt32(data, 32);
            HasChildren = data[36];

            Debug.Assert(data.Length >= OffsetToUSN + 2 + USNSizeWords * 2);

            USNNumber = new byte[2];
            Array.Copy(data, OffsetToUSN, USNNumber, 0, 2);

            USNData = new byte[USNSizeWords * 2];
            Array.Copy(data, OffsetToUSN + 2, USNData, 0, USNSizeWords * 2);

            // Patch USN Data
            ApplyUSNPatch(data, ((int)SizeOfIndexAllocated + 24) / ntfs.Boot.BytesPrSector, ntfs.Boot.BytesPrSector);

            Debug.Assert(SizeOfIndexTotal <= data.Length);

            // Parse entries
            List<IndexEntry> entries = new List<IndexEntry>();

            int pointer = (int)(OffsetToFirstIndex + 24);       // Offset is relative to 0x18
            while (pointer <= SizeOfIndexTotal + 24)
            {
                IndexEntry entry = IndexEntry.ParseData(data, (int)SizeOfIndexTotal - pointer + 24, pointer);

                if (entry.Flags.HasFlag(MFTIndexEntryFlags.LastEntry))
                    break;

                entries.Add(entry);

                pointer += entry.Size;
            }

            Entries = entries.ToArray();
        }

        private void ApplyUSNPatch(byte[] data, int sectors, ushort bytesPrSector)
        {
            Debug.Assert(data.Length >= sectors * bytesPrSector);

            for (int i = 0; i < sectors; i++)
            {
                // Get pointer to the last two bytes
                int blockOffset = i * bytesPrSector + 510;

                // Check that they match the USN Number
                Debug.Assert(data[blockOffset] == USNNumber[0]);
                Debug.Assert(data[blockOffset + 1] == USNNumber[1]);

                // Patch in new data
                data[blockOffset] = USNData[i * 2];
                data[blockOffset + 1] = USNData[i * 2 + 1];
            }
        }
    }
}