using System.Collections.Generic;
using System.Diagnostics;
using NTFSLib.NTFS;
using NTFSLib.Objects.Enums;
using NTFSLib.Utilities;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeExtendedAttributes : Attribute
    {
        public ExtendedAttribute[] ExtendedAttributes { get; set; }

        public override AttributeResidentAllow AllowedResidentStates
        {
            get
            {
                return AttributeResidentAllow.Resident | AttributeResidentAllow.NonResident;
            }
        }

        internal override void ParseAttributeNonResidentBody(INTFSInfo ntfsInfo)
        {
            base.ParseAttributeNonResidentBody(ntfsInfo);

            // Get all chunks
            byte[] data = NtfsUtils.ReadFragments(ntfsInfo, NonResidentHeader.Fragments);

            // Parse
            Debug.Assert(data.Length >= 8);

            List<ExtendedAttribute> extendedAttributes = new List<ExtendedAttribute>();
            int pointer = 0;
            while (pointer + 8 <= data.Length)       // 8 is the minimum size of an ExtendedAttribute
            {
                if (ExtendedAttribute.GetSize(data, pointer) <= 0)
                    break;

                ExtendedAttribute ea = ExtendedAttribute.ParseData(data, (int)NonResidentHeader.ContentSize, pointer);

                extendedAttributes.Add(ea);

                pointer += ea.Size;
            }

            ExtendedAttributes = extendedAttributes.ToArray();
        }

        internal override void ParseAttributeResidentBody(byte[] data, int maxLength, int offset)
        {
            base.ParseAttributeResidentBody(data, maxLength, offset);

            Debug.Assert(maxLength >= 8);

            List<ExtendedAttribute> extendedAttributes = new List<ExtendedAttribute>();
            int pointer = offset;
            while (pointer + 8 <= offset + maxLength)       // 8 is the minimum size of an ExtendedAttribute
            {
                if (ExtendedAttribute.GetSize(data, pointer) < 0)
                    break;

                ExtendedAttribute ea = ExtendedAttribute.ParseData(data, (int) ResidentHeader.ContentLength, pointer);

                extendedAttributes.Add(ea);

                pointer +=ea.Size;
            }

            ExtendedAttributes = extendedAttributes.ToArray();
        }
    }
}