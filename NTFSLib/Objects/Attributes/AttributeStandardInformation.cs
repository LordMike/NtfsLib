using System;
using System.Diagnostics;
using System.IO;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeStandardInformation : Attribute
    {
        public DateTime TimeCreated { get; set; }
        public DateTime TimeModified { get; set; }
        public DateTime TimeMftModified { get; set; }
        public DateTime TimeAccessed { get; set; }
        public FileAttributes DosPermissions { get; set; }
        public uint MaxmiumVersions { get; set; }
        public uint VersionNumber { get; set; }
        public uint ClassId { get; set; }
        public uint OwnerId { get; set; }
        public uint SecurityId { get; set; }
        public ulong QuotaCharged { get; set; }
        public ulong USN { get; set; }

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

            Debug.Assert(maxLength >= 48);

            TimeCreated = Utils.FromWinFileTime(data, offset);
            TimeModified = Utils.FromWinFileTime(data, offset + 8);
            TimeMftModified = Utils.FromWinFileTime(data, offset + 16);
            TimeAccessed = Utils.FromWinFileTime(data, offset + 24);
            DosPermissions = (FileAttributes)BitConverter.ToInt32(data, offset + 32);

            MaxmiumVersions = BitConverter.ToUInt32(data, offset + 36);
            VersionNumber = BitConverter.ToUInt32(data, offset + 40);
            ClassId = BitConverter.ToUInt32(data, offset + 44);

            // The below fields are for version 3.0+
            if (NonResidentFlag == ResidentFlag.Resident && ResidentHeader.ContentLength >= 72)
            {
                Debug.Assert(maxLength >= 72);

                OwnerId = BitConverter.ToUInt32(data, offset + 48);
                SecurityId = BitConverter.ToUInt32(data, offset + 52);
                QuotaCharged = BitConverter.ToUInt64(data, offset + 56);
                USN = BitConverter.ToUInt64(data, offset + 64);
            }
        }
    }
}