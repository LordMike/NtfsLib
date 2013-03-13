using System;
using System.Collections.Generic;
using System.Diagnostics;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeIndexRoot : Attribute
    {
        // Root
        public AttributeType IndexType { get; set; }
        public uint CollationRule { get; set; }
        public uint IndexAllocationSize { get; set; }
        public byte ClustersPrIndexRecord { get; set; }

        // Header
        public uint OffsetToFirstIndex { get; set; }
        public uint SizeOfIndexTotal { get; set; }
        public uint SizeOfIndexAllocated { get; set; }
        public MFTIndexRootFlags IndexFlags { get; set; }

        public IndexEntry[] Entries { get; set; }

        public override AttributeResidentAllow AllowedResidentStates
        {
            get
            {
                return AttributeResidentAllow.Resident;
            }
        }

        internal override void ParseAttributeResidentBody(byte[] data, int maxLength, int offset)
        {
            base.ParseAttributeResidentBody(data, maxLength, offset);

            Debug.Assert(maxLength >= 32);

            IndexType = (AttributeType)BitConverter.ToUInt32(data, offset);
            CollationRule = BitConverter.ToUInt32(data, offset + 4);
            IndexAllocationSize = BitConverter.ToUInt32(data, offset + 8);
            ClustersPrIndexRecord = data[offset + 12];

            OffsetToFirstIndex = BitConverter.ToUInt32(data, offset + 16);
            SizeOfIndexTotal = BitConverter.ToUInt32(data, offset + 20);
            SizeOfIndexAllocated = BitConverter.ToUInt32(data, offset + 24);
            IndexFlags = (MFTIndexRootFlags)data[offset + 28];

            List<IndexEntry> entries = new List<IndexEntry>();

            // Parse entries
            int pointer = offset + 32;
            while (pointer <= offset + SizeOfIndexTotal + 32)
            {
                IndexEntry entry = IndexEntry.ParseData(data, (int)SizeOfIndexTotal - (pointer - offset) + 32, pointer);

                if (entry.Flags.HasFlag(MFTIndexEntryFlags.LastEntry))
                    break;

                entries.Add(entry);

                pointer += entry.Size;
            } 

            Entries = entries.ToArray();
        }
    }
}