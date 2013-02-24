using System;
using System.Diagnostics;
using System.IO;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeStandardInformation : Attribute
    {
        public DateTime CTime { get; set; }
        public DateTime ATime { get; set; }
        public DateTime MTime { get; set; }
        public DateTime RTime { get; set; }
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

            CTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(data, offset));
            ATime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(data, offset + 8));
            MTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(data, offset + 16));
            RTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(data, offset + 24));
            DosPermissions = (FileAttributes)BitConverter.ToUInt32(data, offset + 32);

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