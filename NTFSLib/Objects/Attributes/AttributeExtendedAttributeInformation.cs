using System;
using System.Diagnostics;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeExtendedAttributeInformation : Attribute
    {
        public ushort SizePackedEA { get; set; }
        public ushort CountNeedEA { get; set; }
        public uint SizeUnpackedEA { get; set; }

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

            Debug.Assert(maxLength >= 8);

            SizePackedEA = BitConverter.ToUInt16(data, offset);
            CountNeedEA = BitConverter.ToUInt16(data, offset + 2);
            SizeUnpackedEA = BitConverter.ToUInt32(data, offset + 4);
        }
    }
}