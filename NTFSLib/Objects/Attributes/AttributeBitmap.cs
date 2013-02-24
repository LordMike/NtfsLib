using System;
using System.Collections;
using System.Diagnostics;
using NTFSLib.Objects.Enums;
using NTFSLib.Provider;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeBitmap : Attribute
    {
        public BitArray Bitfield { get; set; }

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

            Debug.Assert(maxLength >= 1);

            byte[] tmpData = new byte[maxLength];
            Array.Copy(data, offset, tmpData, 0, maxLength);

            Bitfield = new BitArray(tmpData);
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
            Bitfield = new BitArray(data);
        }
    }
}