using System;
using System.Diagnostics;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeObjectId : Attribute
    {
        public Guid ObjectId { get; set; }
        public Guid BithVolumeId { get; set; }
        public Guid BithObjectId { get; set; }
        public Guid DomainId { get; set; }

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

            byte[] guidBytes = new byte[16];

            Array.Copy(data, offset, guidBytes, 0, 16);
            ObjectId = new Guid(guidBytes);

            // Parse as much as possible
            if (maxLength < 32)
                return;

            Array.Copy(data, offset + 16, guidBytes, 0, 16);
            BithVolumeId = new Guid(guidBytes);

            if (maxLength < 48)
                return;

            Array.Copy(data, offset + 32, guidBytes, 0, 16);
            BithObjectId = new Guid(guidBytes);

            if (maxLength < 64)
                return;

            Array.Copy(data, offset + 48, guidBytes, 0, 16);
            DomainId = new Guid(guidBytes);
        }
    }
}