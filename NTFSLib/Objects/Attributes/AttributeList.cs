using System;
using System.Collections.Generic;
using System.Diagnostics;
using NTFSLib.Objects.Enums;
using NTFSLib.Provider;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeList : Attribute
    {
        public AttributeListItem[] Items { get; set; }

        public override AttributeResidentAllow AllowedResidentStates
        {
            get
            {
                return AttributeResidentAllow.Resident | AttributeResidentAllow.NonResident;
            }
        }

        internal override void ParseAttributeResidentBody(byte[] data, int maxLength, int offset)
        {
            base.ParseAttributeResidentBody(data, maxLength, offset);

            Debug.Assert(maxLength >= ResidentHeader.ContentLength);

            List<AttributeListItem> results = new List<AttributeListItem>();

            int pointer = offset;
            while (pointer + 26 <= offset + maxLength)      // 26 is the smallest possible MFTAttributeListItem
            {
                AttributeListItem item = AttributeListItem.ParseListItem(data, Math.Min(data.Length - pointer, maxLength), pointer);

                if (item.Type == AttributeType.EndOfAttributes)
                    break;

                results.Add(item);

                pointer += item.Length;
            }

            Items = results.ToArray();
        }

        internal override void ParseAttributeNonResidentBody(IMFTProvider provider)
        {
            base.ParseAttributeNonResidentBody(provider);

            byte[] data = new byte[NonResidentHeader.ContentSize];

            // Get all chunks
            foreach (DataFragment fragment in NonResidentHeader.NonResidentFragments)
            {
                byte[] fragmentData = provider.Read(fragment.LCN, (int)fragment.ClusterCount);

                int clusterSize = fragmentData.Length / (int)fragment.ClusterCount;
                int destinationOffset = (int)fragment.StartingVCN * clusterSize;

                Array.Copy(fragmentData, 0, data, destinationOffset, Math.Min(fragmentData.Length, data.Length - destinationOffset));
            }

            // Parse
            List<AttributeListItem> results = new List<AttributeListItem>();

            int pointer = 0;
            while (pointer + 26 <= data.Length)     // 26 is the smallest possible MFTAttributeListItem
            {
                AttributeListItem item = AttributeListItem.ParseListItem(data, data.Length - pointer, pointer);

                if (item.Type == AttributeType.EndOfAttributes)
                    break;

                results.Add(item);

                pointer += item.Length;
            }

            Items = results.ToArray();
        }
    }
}