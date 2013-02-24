using System;
using System.Diagnostics;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeVolumeInformation : Attribute
    {
        public ulong Reserved { get; set; }
        public byte MajorVersion { get; set; }
        public byte MinorVersion { get; set; }
        public VolumeInformationFlags VolumeInformationFlag { get; set; }

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

            Debug.Assert(maxLength >= 16);

            Reserved = BitConverter.ToUInt64(data, offset);
            MajorVersion = data[offset + 8];
            MinorVersion = data[offset + 9];
            VolumeInformationFlag = (VolumeInformationFlags)BitConverter.ToUInt16(data, offset + 10);
        }
    }
}