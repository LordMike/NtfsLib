using System.Collections.Generic;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Specials;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeIndexAllocation : Attribute
    {
        public IndexAllocationChunk[] Indexes { get; set; }
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
            byte[] data = Utils.ReadFragments(ntfs, NonResidentHeader.Fragments);

            List<IndexAllocationChunk> indexes = new List<IndexAllocationChunk>();
            List<IndexEntry> entries = new List<IndexEntry>();

            // Parse
            for (int i = 0; i < NonResidentHeader.Fragments.Length; i++)
            {
                for (int j = 0; j < NonResidentHeader.Fragments[i].Clusters; j++)
                {
                    int offset = (int)(NonResidentHeader.Fragments[i].StartingVCN * ntfs.BytesPrCluster + j * ntfs.BytesPrCluster);
                    
                    if (!IndexAllocationChunk.IsIndexAllocationChunk(data, offset))
                        continue;

                    IndexAllocationChunk index = IndexAllocationChunk.ParseBody(ntfs, data, offset);

                    indexes.Add(index);
                    entries.AddRange(index.Entries);
                }
            }

            Indexes = indexes.ToArray();
            Entries = entries.ToArray();
        }
    }
}