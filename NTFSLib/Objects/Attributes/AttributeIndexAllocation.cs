using System.Collections.Generic;
using NTFSLib.NTFS;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Specials;
using NTFSLib.Utilities;

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

        internal override void ParseAttributeNonResidentBody(NTFSWrapper ntfsWrapper)
        {
            byte[] data = NtfsUtils.ReadFragments(ntfsWrapper, NonResidentHeader.Fragments);

            List<IndexAllocationChunk> indexes = new List<IndexAllocationChunk>();
            List<IndexEntry> entries = new List<IndexEntry>();

            // Parse
            for (int i = 0; i < NonResidentHeader.Fragments.Length; i++)
            {
                for (int j = 0; j < NonResidentHeader.Fragments[i].Clusters; j++)
                {
                    int offset = (int)((NonResidentHeader.Fragments[i].StartingVCN - NonResidentHeader.Fragments[0].StartingVCN) * ntfsWrapper.BytesPrCluster + j * ntfsWrapper.BytesPrCluster);
                    
                    if (!IndexAllocationChunk.IsIndexAllocationChunk(data, offset))
                        continue;

                    IndexAllocationChunk index = IndexAllocationChunk.ParseBody(ntfsWrapper, data, offset);

                    indexes.Add(index);
                    entries.AddRange(index.Entries);
                }
            }

            Indexes = indexes.ToArray();
            Entries = entries.ToArray();
        }
    }
}